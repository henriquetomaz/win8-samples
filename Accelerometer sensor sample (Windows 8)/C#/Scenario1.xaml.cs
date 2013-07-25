#define TEST

//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

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
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Scenario1 : SDKTemplate.Common.LayoutAwarePage
    {
        // A pointer back to the main page.  This is needed if you want to call methods in MainPage such
        // as NotifyUser()
        MainPage rootPage = MainPage.Current;
        IDisposable _subscription;

        public Scenario1()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached. The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ScenarioEnableButton.IsEnabled = true;
            ScenarioDisableButton.IsEnabled = false;
        }

        private void Disable()
        {
            Window.Current.VisibilityChanged -= new WindowVisibilityChangedEventHandler(VisibilityChanged);

            if (_subscription != null) {
                _subscription.Dispose();
                _subscription = null;
                //statusTextBlock.Text = "Accelerometer Stopped";
            }
        }

        /// <summary>
        /// Invoked immediately before the Page is unloaded and is no longer the current source of a parent Frame.
        /// </summary>
        /// <param name="e">
        /// Event data that can be examined by overriding code. The event data is representative
        /// of the navigation that will unload the current Page unless canceled. The
        /// navigation can potentially be canceled by setting Cancel.
        /// </param>
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (ScenarioDisableButton.IsEnabled)
                Disable();

            base.OnNavigatingFrom(e);
        }

        /// <summary>
        /// This is the event handler for VisibilityChanged events. You would register for these notifications
        /// if handling sensor data when the app is not visible could cause unintended actions in the app.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">
        /// Event data that can be examined for the current visibility state.
        /// </param>
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
        /// This is the event handler for ReadingChanged events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async private void ReadingChanged(object sender, AccelerometerReadingChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                AccelerometerReading reading = e.Reading;
                ScenarioOutput_X.Text = String.Format("{0,5:0.00}", reading.AccelerationX);
                ScenarioOutput_Y.Text = String.Format("{0,5:0.00}", reading.AccelerationY);
                ScenarioOutput_Z.Text = String.Format("{0,5:0.00}", reading.AccelerationZ);
            });
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
            Window.Current.VisibilityChanged += new WindowVisibilityChangedEventHandler(VisibilityChanged);

#if TEST
            var obs = AccelerometerObservable.Emulate();
            _subscription =
                obs.ObserveOnDispatcher()
                    .Subscribe(UpdateUI);

            AccelerometerObservable.FindBigMovements(obs).Subscribe(Update2);
#else
            _subscription =
                AccelerometerObservable.Instance
                    .Sample(TimeSpan.FromMilliseconds(100))
                    .ObserveOnDispatcher()
                    .Subscribe(UpdateUI);
#endif
         }

        private void Update2(Vector v)
        {
            magLine.X2 = magLine.X1 + v.Length() * 100;
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
    }
}
