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


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace StockTiles
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const string TREND_UP = "▲";
        const string TREND_DOWN = "▼";
        const string TREND_NEUTRAL = "-";

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void UpdateValue(StockData stock, double newValue)
        {
            var delta = newValue - stock.Price;
            stock.Price = newValue;

            Debug.WriteLine("new price: {0},    delta: {1}", stock.Price, delta);
            double percentChange = Math.Round(delta / stock.Price * 100, 1);

            var icon = delta < 0 ? TREND_DOWN : TREND_UP;

            stock.TrendTick = String.Format("{0}   {1:0.00}  ({2:0.0%})  ", icon, delta, delta / stock.Price);
        }

        public void UpdateMovingAvg(StockData stock, double newValue)
        {
            Debug.WriteLine("new moving average: {0}", newValue);

            //double percentChange = Math.Round(delta / stock.Price * 100, 1);
            double delta = newValue - stock.OpenPrice;

            var icon = delta < 0 ? TREND_DOWN : TREND_UP;

            stock.MovingAvg30Sec = String.Format("{0}   {1:0.00}  ({2:0.0%})  ", icon, delta, newValue);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var data1 = new StockData()
            {
                Name = "MSFT",
                Price = 34.50,
            };

            var data2 = new StockData()
            {
                Name = "AAPL",
                Price = 500.0,
            };

            liveTile1.DataContext = data1;
            liveTile2.DataContext = data2;


            var obs1 = GetSimulatedTicker(34, 0.05).Publish().RefCount();
            var obs2 = GetSimulatedTicker(500, 0.01).Publish().RefCount();
            
            obs1.ObserveOnDispatcher().Subscribe(x => UpdateValue(data1, x));
            
            obs2.ObserveOnDispatcher().Subscribe(x => UpdateValue(data2, x));

            //var q = from w in obs1.Window(TimeSpan.FromMilliseconds(1000))
            //        from x in w.Average()
            //        select x;

            obs1.Window(TimeSpan.FromMilliseconds(30000), TimeSpan.FromMilliseconds(1000))
                .SelectMany(x => x.Average())
                .ObserveOnDispatcher().Subscribe(x => UpdateMovingAvg(data1, x));


            //var nextVal1 = obs1.Skip(1);
            //obs1.Zip(nextVal1, (prev, curr) => curr - prev)
            //    .ObserveOnDispatcher()
            //    .Subscribe(x => UpdateTrend(data1, x));

            

            //var stockObs = await WebSocketSubject.Create("ws://localhost:8080");
            //stockObs.Subscribe(next => Debug.WriteLine(next));
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
                    .Scan(initialValue, (acc, current) => acc + current);
        }

        //var prices = Observable.Generate(
        //    5d,
        //    i => i > 0,
        //    i => i + rand.NextDouble() - 0.5,
        //    i => i,
        //    i => TimeSpan.FromSeconds(0.1)
        //);
    

        //private IObservable<double> GetStockObservable(string tickerSymbol)
        //{
        //    /* Subscribe to the StockTickerWatcher for the particular symbol. 
        //       To display, throttle based on what the update frequency should be.
        //       To detect a spike/dip in the stock price, have a different observable that 
        //     * keeps some number of events. (use scan?  buffer?  window?)
        //     * Have another observable source that combines all the stock updates and displays the
        //     * percent changed in the total portfolio.
        //     * Then also add an observable that detects big changes.
        //     * 
        //     * 
        //     */
        //     up or down: since open, tick, 5 seconds, 10 seconds
        //     percentage changed over all stocks
        //}
    }

    //class StockTickerWatcher
    //{
    //    /* Connect to the stock ticker service and get ticker results for whatever ticker symbols have 
    //     * been requested to date. If service pushes new data, just use OnNext there. If service must be
    //     * polled, then poll based on an observable.interval(timespan)
    //     * ? (Can also cache results from each tick response)
    //    */

    //    public IObservable<Tuple<string, double>> getStockObservable()
    //    {
    //        var observable = 
    //    }
    //}
}
