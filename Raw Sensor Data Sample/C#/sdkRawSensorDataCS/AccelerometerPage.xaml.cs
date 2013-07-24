using System;
using Microsoft.Phone.Controls;

using System.Windows.Threading;

using System.Reactive.Linq;
using System.Reactive;
using Microsoft.Xna.Framework;

using Windows.Foundation;
using Windows.Devices.Sensors;

using System.Diagnostics;

namespace sdkRawSensorDataCS
{
    using ReadingChangedHandler = TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs>;

    public class AccelerometerObservable
    {
        private readonly Accelerometer _accel;
        private readonly IObservable<AccelerometerReading> _accelObs;
        readonly uint _reportInterval;
        const uint MIN_REPORT_INTERVAL = 16;

        private static Lazy<AccelerometerObservable> _instanceLazy = new Lazy<AccelerometerObservable>(() => new AccelerometerObservable());
        public static IObservable<AccelerometerReading> Instance { get { return _instanceLazy.Value._accelObs; } }

        private AccelerometerObservable()
        {
            _accel = Accelerometer.GetDefault();            

            _reportInterval = _accel.MinimumReportInterval;
            if (_reportInterval < MIN_REPORT_INTERVAL)
                _reportInterval = MIN_REPORT_INTERVAL;

            #region + Event Subscriptions +
            Action<ReadingChangedHandler> subscribeEvent =
                h => {
                    _accel.ReadingChanged += h;
                    if (_accel.ReportInterval < _reportInterval)
                        _accel.ReportInterval = _reportInterval;
                };

            Action<ReadingChangedHandler> unsubscribeEvent =
                h => {
                    _accel.ReadingChanged -= h;
                    _accel.ReportInterval = 0;
                };
            #endregion 

            if (_accel != null) {
                _accelObs =
                    Observable.FromEventPattern<ReadingChangedHandler, AccelerometerReadingChangedEventArgs>
                    (subscribeEvent, unsubscribeEvent)
                    //.Do(l => Debug.WriteLine("Publishing {0}", l.EventArgs.Reading.Timestamp)) //side effect to show it is running
                    .Select(x => x.EventArgs.Reading)
                    .Publish()
                    .RefCount();
            } 
            else {
                _accelObs = Observable.Empty<AccelerometerReading>();
            }
        }

        void StartReading()
        {
            _accel.ReportInterval = _reportInterval;
        }
    }

    public partial class AccelerometerPage : PhoneApplicationPage
    {

        //.Sample(TimeSpan.FromMilliseconds(200))
        // .ObserveOnDispatcher();


        //DispatcherTimer timer;
        bool isDataValid;
        IDisposable _subscription;

        //timer = new DispatcherTimer();
        //timer.Interval = TimeSpan.FromMilliseconds(30);
        //timer.Tick += new EventHandler(timer_Tick);

        public AccelerometerPage()
        {
            InitializeComponent();

            //if (!Accelerometer.IsSupported)
            //{
            //    // The device on which the application is running does not support
            //    // the accelerometer sensor. Alert the user and hide the
            //    // application bar.
            //    statusTextBlock.Text = "device does not support accelerometer";
            //    ApplicationBar.IsVisible = false;
            //}
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            if (_subscription != null) {
                _subscription.Dispose();
                _subscription = null;
                statusTextBlock.Text = "accelerometer stopped.";
            }

            else {
                _subscription =
                    AccelerometerObservable.Instance
                    .Sample(TimeSpan.FromMilliseconds(100))
                    .ObserveOnDispatcher()
                    .Subscribe(UpdateUI);
            }

        }

        private void UpdateUI(AccelerometerReading reading)
        {
            statusTextBlock.Text = "receiving data from accelerometer.";

            // Show the numeric values
            xTextBlock.Text = "X: " + reading.AccelerationX.ToString("0.00");
            yTextBlock.Text = "Y: " + reading.AccelerationY.ToString("0.00");
            zTextBlock.Text = "Z: " + reading.AccelerationZ.ToString("0.00");

            // Show the values graphically
            xLine.X2 = xLine.X1 + reading.AccelerationX * 100;
            yLine.Y2 = yLine.Y1 - reading.AccelerationY * 100;
            zLine.X2 = zLine.X1 - reading.AccelerationZ * 50;
            zLine.Y2 = zLine.Y1 + reading.AccelerationZ * 50;            
        }

        // Note that this event handler is called from a background thread
        // and therefore does not have access to the UI thread. To update 
        // the UI from this handler, use Dispatcher.BeginInvoke() as shown.
        // Dispatcher.BeginInvoke(() => { statusTextBlock.Text = "in CurrentValueChanged"; });
    }

#if false
    public partial class AccelerometerPage : PhoneApplicationPage
    {
        Accelerometer accelerometer;
        DispatcherTimer timer;
        Vector3 acceleration;
        bool isDataValid;

        public AccelerometerPage()
        {
            InitializeComponent();

            if (!Accelerometer.IsSupported)
            {
                // The device on which the application is running does not support
                // the accelerometer sensor. Alert the user and hide the
                // application bar.
                statusTextBlock.Text = "device does not support compass";
                ApplicationBar.IsVisible = false;
            }
            else
            {
                // Initialize the timer and add Tick event handler, but don't start it yet.
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(30);
                timer.Tick += new EventHandler(timer_Tick);
            }
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            if (accelerometer != null && accelerometer.IsDataValid)
            {
                // Stop data acquisition from the accelerometer.
                accelerometer.Stop();
                timer.Stop();
                statusTextBlock.Text = "accelerometer stopped.";

            }
            else
            {
                if (accelerometer == null)
                {
                    // Instantiate the accelerometer.
                    accelerometer = new Accelerometer();


                    // Specify the desired time between updates. The sensor accepts
                    // intervals in multiples of 20 ms.
                    accelerometer.TimeBetweenUpdates = TimeSpan.FromMilliseconds(20);

                    // The sensor may not support the requested time between updates.
                    // The TimeBetweenUpdates property reflects the actual rate.
                    timeBetweenUpdatesTextBlock.Text = accelerometer.TimeBetweenUpdates.TotalMilliseconds + " ms";


                    accelerometer.CurrentValueChanged += new EventHandler<SensorReadingEventArgs<AccelerometerReading>>(accelerometer_CurrentValueChanged);
                }

                try
                {
                    statusTextBlock.Text = "starting accelerometer.";
                    accelerometer.Start();
                    timer.Start();
                }
                catch (InvalidOperationException)
                {
                    statusTextBlock.Text = "unable to start accelerometer.";
                }

            }

        }

        void accelerometer_CurrentValueChanged(object sender, SensorReadingEventArgs<AccelerometerReading> e)
        {
            // Note that this event handler is called from a background thread
            // and therefore does not have access to the UI thread. To update 
            // the UI from this handler, use Dispatcher.BeginInvoke() as shown.
            // Dispatcher.BeginInvoke(() => { statusTextBlock.Text = "in CurrentValueChanged"; });


            isDataValid = accelerometer.IsDataValid;

            acceleration = e.SensorReading.Acceleration;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (isDataValid)
            {
                statusTextBlock.Text = "receiving data from accelerometer.";

                // Show the numeric values
                xTextBlock.Text = "X: " + acceleration.X.ToString("0.00");
                yTextBlock.Text = "Y: " + acceleration.Y.ToString("0.00");
                zTextBlock.Text = "Z: " + acceleration.Z.ToString("0.00");

                // Show the values graphically
                xLine.X2 = xLine.X1 + acceleration.X * 100;
                yLine.Y2 = yLine.Y1 - acceleration.Y * 100;
                zLine.X2 = zLine.X1 - acceleration.Z * 50;
                zLine.Y2 = zLine.Y1 + acceleration.Z * 50;
            }
        }
    }
#endif
}