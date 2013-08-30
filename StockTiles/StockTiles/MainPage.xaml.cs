using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Networking.Sockets;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StockTiles
{
    public sealed partial class MainPage : Page
    {
        const string TREND_UP = "▲";
        const string TREND_UP_SMALL = "▴";
        const string TREND_DOWN = "▼";
        const string TREND_DOWN_SMALL = "▾";
        const string TREND_NEUTRAL = "-";

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void UpdateValue(StockData stock, double newValue)
        {

        }

        // MSFT 
        // 34 (^ 0.1  5%  since open)
        // ^ 0.1 (last tick)
        //
        // moving averages
        // 33  (30 second)
        // 33.5 (60 second)

        // [Aggregate]
        // ^ 5%  (since open)
        // ^ 10% (60 second)

        private static string FormatDelta(double oldPrice, double newPrice, bool showPercent = false)
        {
            var delta = newPrice - oldPrice;

            Debug.WriteLine("new price: {0},    delta: {1}", newPrice, delta);

            #region Triangle display
            string triangle;
            if (showPercent)
                triangle = delta < 0 ? TREND_DOWN : TREND_UP;
            else
                triangle = delta < 0 ? TREND_DOWN_SMALL : TREND_UP_SMALL;
            #endregion

            string percentString = showPercent ? String.Format("{0:0.0%}", delta / oldPrice) : "";

            return String.Format("{0}   {1:0.00}   {2:0.0%}", triangle, Math.Abs(delta), percentString);
        }

        private string FormatMovingAvg(StockData stock, double newValue)
        {
            Debug.WriteLine("new moving average: {0}", newValue);

            //double percentChange = Math.Round(delta / stock.Price * 100, 1);
            double delta = newValue - stock.OpenPrice;
            var icon = delta < 0 ? TREND_DOWN : TREND_UP;
            return String.Format("{0}   {1:0.00}  ({2:0.0%})  ", icon, delta, newValue);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var data1 = new StockData() {
                Name = "MSFT",
                OpenPrice = 34.50,
                variance = 0.05,
            };

            var data2 = new StockData() {
                Name = "AAPL",
                OpenPrice = 490.0,
                variance = 0.1,
            };

            var dataList = new[] { data1, data2 };
            itemGridView.ItemsSource = dataList;

            foreach (var item in dataList) {
                CreateObservable(item);
            }
        }

        private void CreateObservable(StockData stock)
        {
            var ticker = GetSimulatedTicker(stock.OpenPrice, stock.variance);

            ticker
                .ObserveOnDispatcher()
                .Subscribe(
                    x => {
                        stock.OpenDelta = FormatDelta(stock.OpenPrice, x, showPercent: true);
                        stock.TickDelta = FormatDelta(stock.Price, x);
                        stock.Price = x;
                    }
                );

            ticker
                .Window(TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(1000))  // 30 second moving average that moves forward every second
                .SelectMany(x => x.Average())
                .ObserveOnDispatcher()
                .Subscribe(
                    x => stock.MovingAvg30Sec = String.Format("{0:0.00}", x)
                );

            ticker
                .Window(TimeSpan.FromSeconds(60), TimeSpan.FromMilliseconds(1000))  // 60 second moving average that moves forward every second
                .SelectMany(x => x.Average())
                .ObserveOnDispatcher()
                .Subscribe(
                    x => stock.MovingAvg1Min = String.Format("{0:0.00}", x)
                );
        }

        public static IObservable<double> GetSimulatedTicker(double initialValue, double variance)
        {
            Random random = new Random();

            return Observable
                    .Interval(TimeSpan.FromMilliseconds(1000))
                    .Select(x =>
                    {
                        var nextRand = random.NextDouble();
                        var newVal = 0.0;

                        if (nextRand > .90)
                            newVal = variance * 10;
                        else if (nextRand > .50)
                            newVal = variance * nextRand;
                        else
                            newVal = -variance * nextRand;

                        return Math.Round(newVal, 2);
                    })
                    .Scan(initialValue, (acc, current) => acc + current)
                    .Publish()
                    .RefCount();
        }


        //var prices = Observable.Generate(
        //    5d,
        //    i => i > 0,
        //    i => i + rand.NextDouble() - 0.5,
        //    i => i,
        //    i => TimeSpan.FromSeconds(0.1)
        //);
    

        //     percentage changed over all stocks
    }

}
