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
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using ReactiveUI.Xaml;
using ReactiveUI;

namespace Microsoft.Samples.Devices.Sensors.AccelerometerSample
{
    public sealed partial class Scenario1 : SDKTemplate.Common.LayoutAwarePage
    {
        MainPage rootPage = MainPage.Current;
        IDisposable readingSubscription = Disposable.Empty;
        BehaviorSubject<bool> isEnabled = new BehaviorSubject<bool>(true);
        ReactiveCommand enableCommand, disableCommand, simulateData;

        public Scenario1()
        {
            this.InitializeComponent();
            enableCommand = new ReactiveCommand(isEnabled);
            disableCommand = new ReactiveCommand(isEnabled.Select(b => !b));
            simulateData = new ReactiveCommand(isEnabled);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ScenarioEnableButton.Command = enableCommand;
            ScenarioDisableButton.Command = disableCommand;
            SimulateDataCheckbox.Command = simulateData;

            enableCommand.Subscribe(
                x => { 
                    isEnabled.OnNext(false); 
                    Enable(); 
                });

            disableCommand.Subscribe(
                x => { 
                    isEnabled.OnNext(true); 
                    Disable(); 
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
            Window.Current.VisibilityChanged += VisibilityChanged;

            if (SimulateDataCheckbox.IsChecked.GetValueOrDefault(false))
            {
                readingSubscription =
                    EmulateSensor.Emulate()
                        .ObserveOnDispatcher()
                        .Subscribe(UpdateUI);
            }
            else
            {
                readingSubscription =
                    AccelerometerObservable.Instance
                        .Sample(TimeSpan.FromMilliseconds(100))
                        .ObserveOnDispatcher()
                        .Subscribe(UpdateUI);
            }

            //AccelerometerObservable.FindBigMovements(obs).Subscribe(Update2);
        }

        private void Disable()
        {
            Window.Current.VisibilityChanged -= VisibilityChanged;

            if (readingSubscription != null) {
                readingSubscription.Dispose();
                readingSubscription = null;
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            isEnabled.OnNext(false);
            Disable();

            base.OnNavigatingFrom(e);
        }

        private void VisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
            isEnabled.OnNext(e.Visible);
        }
    }
}
