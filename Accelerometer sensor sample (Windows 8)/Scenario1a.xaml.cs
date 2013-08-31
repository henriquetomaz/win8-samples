using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using SDKTemplate;
using System;
using Windows.Devices.Sensors;
using Windows.Foundation;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace Microsoft.Samples.Devices.Sensors.AccelerometerSample
{
    public sealed partial class Scenario1a : SDKTemplate.Common.LayoutAwarePage
    {
        MainPage rootPage = MainPage.Current;

        private Accelerometer _accelerometer;
        private uint _desiredReportInterval;

        public Scenario1a()
        {
            this.InitializeComponent();

            _accelerometer = Accelerometer.GetDefault();
            if (_accelerometer != null)
            {
                uint minReportInterval = _accelerometer.MinimumReportInterval;
                _desiredReportInterval = minReportInterval > 16 ? minReportInterval : 16;
            }
            else
            {
                rootPage.NotifyUser("No accelerometer found", NotifyType.StatusMessage);
            }
        }

        private void Enable()
        {
            // Establish the report interval
            _accelerometer.ReportInterval = _desiredReportInterval;

            Window.Current.VisibilityChanged += VisibilityChanged;
            _accelerometer.ReadingChanged += ReadingChanged;
        }

        private void Disable()
        {
            Window.Current.VisibilityChanged -= new WindowVisibilityChangedEventHandler(VisibilityChanged);
            _accelerometer.ReadingChanged -= new TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs>(ReadingChanged);

            // Restore the default report interval to release resources while the sensor is not in use
            _accelerometer.ReportInterval = 0;
        }

        async private void ReadingChanged(object sender, AccelerometerReadingChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                AccelerometerReading reading = e.Reading;
                ScenarioOutput_X.Text = String.Format("{0,5:0.00}", reading.AccelerationX);
                ScenarioOutput_Y.Text = String.Format("{0,5:0.00}", reading.AccelerationY);
                ScenarioOutput_Z.Text = String.Format("{0,5:0.00}", reading.AccelerationZ);
            });
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ScenarioEnableButton.IsEnabled = true;
            ScenarioDisableButton.IsEnabled = false;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (ScenarioDisableButton.IsEnabled)
                Disable();

            base.OnNavigatingFrom(e);
        }

        private void VisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
            if (ScenarioEnableButton.IsEnabled)
            {
                if (e.Visible)
                    Enable();
                else
                    Disable();
            }
        }


        private void ScenarioEnable(object sender, RoutedEventArgs e)
        {
            if (_accelerometer != null)
            {
                Enable();

                ScenarioEnableButton.IsEnabled = false;
                ScenarioDisableButton.IsEnabled = true;
            }
            else
            {
                rootPage.NotifyUser("No accelerometer found", NotifyType.StatusMessage);
            }
        }

        private void ScenarioDisable(object sender, RoutedEventArgs e)
        {
            Disable();

            ScenarioEnableButton.IsEnabled = true;
            ScenarioDisableButton.IsEnabled = false;
        }
    }
}
