using System;
using Microsoft.Phone.Controls;

using System.Windows.Threading;

using System.Reactive.Linq;
using System.Reactive;
using Microsoft.Xna.Framework;

using Windows.Foundation;
using Windows.Devices.Sensors;

using System.Diagnostics;
using SensorData;

namespace sdkRawSensorDataCS
{
    public partial class AccelerometerPage : PhoneApplicationPage
    {
        IDisposable _subscription;

        public AccelerometerPage()
        {
            InitializeComponent();
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            if (_subscription != null) {
                _subscription.Dispose();
                _subscription = null;
                statusTextBlock.Text = "Accelerometer Stopped";
            }

            else {
                //var source = AccelerometerObservable.Emulate();
                var source = AccelerometerObservable.Instance;

                _subscription = 
                    source                   
                    .Throttle(TimeSpan.FromMilliseconds(20))
                    .ObserveOnDispatcher()
                    .Subscribe(UpdateUI);
            }

        }

        private void UpdateUI(SensorData.Vector reading)
        {
            statusTextBlock.Text = "Receiving data from accelerometer...";

            // Show the numeric values
            xTextBlock.Text = "X: " + reading.X.ToString("0.00");
            yTextBlock.Text = "Y: " + reading.Y.ToString("0.00");
            zTextBlock.Text = "Z: " + reading.Z.ToString("0.00");

            // Show the values graphically
            xLine.X2 = xLine.X1 + reading.X * 100;
            yLine.Y2 = yLine.Y1 - reading.Y * 100;
            zLine.X2 = zLine.X1 - reading.Z * 50;
            zLine.Y2 = zLine.Y1 + reading.Z * 50;            
        }
    }

}