using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using SDKTemplate;
using System;
using Windows.Devices.Sensors;
using Windows.Foundation;
using System.Threading.Tasks;
using Windows.UI.Core;

using SensorData;

using System.Reactive.Linq;
using System.Reactive;

namespace Microsoft.Samples.Devices.Sensors.AccelerometerSample
{
    public sealed partial class Scenario1 : SDKTemplate.Common.LayoutAwarePage
    {
        MainPage rootPage = MainPage.Current;
        IDisposable _subscription;

        public Scenario1()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ScenarioEnableButton.IsEnabled = true;
            ScenarioDisableButton.IsEnabled = false;
        }

        private void UpdateUI(SensorData.Vector reading)
        {
            ScenarioOutput_X.Text = String.Format("{0,5:0.00}", reading.X);
            ScenarioOutput_Y.Text = String.Format("{0,5:0.00}", reading.Y);
            ScenarioOutput_Z.Text = String.Format("{0,5:0.00}", reading.Z);

            // Show the values graphically
            xLine.X2 = xLine.X1 + reading.X * 100;
            yLine.Y2 = yLine.Y1 - reading.Y * 100;
            zLine.X2 = zLine.X1 - reading.Z * 50;
            zLine.Y2 = zLine.Y1 + reading.Z * 50;
        }

        private void Enable()
        {
            Window.Current.VisibilityChanged += VisibilityChanged;

            if (SimulateDataCheckbox.IsChecked.GetValueOrDefault(false))
            {
                //AccelerometerObservable.FindBigMovements(obs).Subscribe(Update2);
                _subscription =
                    EmulateSensor.Emulate()
                        .ObserveOnDispatcher()
                        .Subscribe(UpdateUI);
            }
            else
            {
                _subscription =
                    AccelerometerObservable.Instance
                        .Sample(TimeSpan.FromMilliseconds(100))
                        .ObserveOnDispatcher()
                        .Subscribe(UpdateUI);
            }
        }

        private void Disable()
        {
            Window.Current.VisibilityChanged -= VisibilityChanged;

            if (_subscription != null) {
                _subscription.Dispose();
                _subscription = null;
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (ScenarioDisableButton.IsEnabled)
                Disable();

            base.OnNavigatingFrom(e);
        }

        private void VisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
            if (ScenarioEnableButton.IsEnabled) {
                if (e.Visible)
                    Enable();
                else
                    Disable();
            }
        }


        /// <summary>
        /// This is the click handler for the 'Enable' button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScenarioEnable(object sender, RoutedEventArgs e)
        {
            Enable();

            ScenarioEnableButton.IsEnabled = false;
            ScenarioDisableButton.IsEnabled = true;
        }

        /// <summary>
        /// This is the click handler for the 'Disable' button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScenarioDisable(object sender, RoutedEventArgs e)
        {
            Disable();

            ScenarioEnableButton.IsEnabled = true;
            ScenarioDisableButton.IsEnabled = false;
        }

        private void SimulateDataCheckbox_Click(object sender, RoutedEventArgs e)
        {
            if (ScenarioEnableButton.IsEnabled)
                Enable();   // switch between simulated and real accelerometer data
            else
                Disable();
        }
    }
}
