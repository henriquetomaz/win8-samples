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
        const string TREND_DOWN = "▼";

        private CombinedData _combinedStats = new CombinedData();
        private StockData[] _stockData;

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _stockData = new[] { 
                new StockData() { Name = "MSFT", OpenPrice = 34.50, variance = 0.10 }, 
                new StockData() { Name = "AAPL", OpenPrice = 490.0, variance = 3.50 },
                new StockData() { Name = "NOK", OpenPrice = 4.0, variance = 0.03 },
            };

            itemGridView.ItemsSource = _stockData;

            foreach (var item in _stockData) {
                InitializeObservable(item);
            }

            combinedTile.DataContext = _combinedStats;
        }

        private void InitializeObservable(StockData stock)
        {
            var ticker = GetSimulatedTicker(stock.OpenPrice, stock.variance);

            // to connect to a real stock ticker, perhaps over websockets, could do:
            // var stockObs = await WebSocketSubject.Create("ws://localhost:8080", stock.Name);
            // ... use stockObs instead of 'ticker'

            ticker
                .ObserveOnDispatcher()
                .Subscribe(
                    x => {
                        stock.OpenDelta = FormatDelta(stock.OpenPrice, x, showPercent: true);
                        stock.UpDownIcon = (x - stock.OpenPrice <= 0) ? TREND_DOWN : TREND_UP;
                        stock.TickDelta = FormatDelta(stock.Price, x);
                        stock.Price = x;
                        stock.PriceString = String.Format("{0:0.00}", x);
                        UpdateCombinedStats();
                    }
                );


            ticker
                .MovingAverage(TimeSpan.FromSeconds(30)) // 30 second moving average that moves forward every second
                .ObserveOnDispatcher()
                .Subscribe(
                    x => stock.MovingAvg30Sec = String.Format("{0:0.00}", x)
                );


            ticker
                .MovingAverage(TimeSpan.FromSeconds(60))
                .ObserveOnDispatcher()
                .Subscribe(
                    x => stock.MovingAvg1Min = String.Format("{0:0.00}", x)
                );
        }

        private void UpdateCombinedStats()
        {
            var combined =
                _stockData
                    .Select(stock => (stock.Price - stock.OpenPrice) / stock.OpenPrice)
                    .Average();

            _combinedStats.CombinedChange = String.Format("{0:0.0%}", combined);
            Debug.WriteLine("combined change: {0}", _combinedStats.CombinedChange);
        }

        public static IObservable<double> GetSimulatedTicker(double initialValue, double variance)
        {
            Random random = new Random();

            var deltas = Observable.Generate(
                initialState: 0d,
                condition: x => true,
                iterate: x => GenerateNextDelta(random.NextDouble(), variance),
                resultSelector: x => x,
                timeSelector: x => TimeSpan.FromMilliseconds(1000)
            );

            return deltas.Scan(initialValue, (acc, current) => acc + current)
                    .Publish()
                    .RefCount();
        }

        #region GenerateNextDelta
        // note: unit of variance is the same as stock price (i.e., dollar change)
        // return value is also dollar change
        private static double GenerateNextDelta(double randValue, double variance)
        {
            var newVal = 0.0;

            if (randValue > .95)
                newVal = variance * 3;
            else if (randValue > .90)
                newVal = -variance * 3;
            else if (randValue > .50)
                newVal = variance * randValue;
            else
                newVal = -variance * randValue;

            return Math.Round(newVal, 2);
        }
        #endregion

        #region FormatDelta
        private static string FormatDelta(double oldPrice, double newPrice, bool showPercent = false)
        {
            var delta = Math.Round(newPrice - oldPrice, 2);
            string percentString = showPercent ? String.Format("({0:+0.0%;-0.0%})", delta / oldPrice) : "";

            return String.Format("{0:+0.00;-0.00}   {1}", delta, percentString);
        }
        #endregion 

        //     TODO: percentage changed over all stocks
    }

    public static class Extension 
    {
        public static IObservable<double> MovingAverage(this IObservable<double> self, TimeSpan duration)
        {
            return self
                    .Window(duration, TimeSpan.FromMilliseconds(1000))  // X second moving average that moves forward every second
                    .SelectMany(x => x.Average());
        }
    }

}
