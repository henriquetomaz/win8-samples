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
    public partial class BeforeRx : PhoneApplicationPage
    {
        Accelerometer _accel;
        //DispatcherTimer _timer;

        Vector _previousValue;

        public BeforeRx()
        {
            InitializeComponent();

            _accel = Accelerometer.GetDefault();
            
            if (_accel == null)
            {
                statusTextBlock.Text = "device does not support accelerometer";
                ApplicationBar.IsVisible = false;
            }
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            if (_accel != null)
            {                                
                statusTextBlock.Text = "accelerometer stopped.";
                _accel.ReportInterval = 0;
                _accel.ReadingChanged -= new TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs>(ReadingChanged);
            }
            else
            {
                _accel.ReportInterval = 16;
                _accel.ReadingChanged += new TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs>(ReadingChanged);
            }
        }

        private void ReadingChanged(Accelerometer sender, AccelerometerReadingChangedEventArgs args)
        {
            var reading = args.Reading;

            Vector current = new Vector(reading.AccelerationX, reading.AccelerationY, reading.AccelerationZ);
            var delta = new Vector(current.X - _previousValue.X, current.Y - _previousValue.Y, current.Z - _previousValue.Z);

            _previousValue = current;

            if (delta.Length() > 1.0) {
                Dispatcher.BeginInvoke(() => {
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
                });
            }
        }
    }
}