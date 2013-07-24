using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Device.Location;

using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

using DataBoundApp1.Resources;
using DataBoundApp1.ViewModels;
using MetalWrench.VegasLocater;

using System.Reactive;
using System.Reactive.Linq;
using Windows.Devices.Sensors;
using Windows.Foundation;

namespace DataBoundApp1
{
    using ReadingChangedHandler = TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs>;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Diagnostics;
    using System.Reactive.Concurrency;

    public partial class MainPage : PhoneApplicationPage
    {
        private bool checkingLocation = false;
        private GeoCoordinateObservable finder;
        private IDisposable subscription = null;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Set the data context of the LongListSelector control to the sample data
            DataContext = App.ViewModel;

            finder = new GeoCoordinateObservable(GeoPositionAccuracy.High, 20);

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        // Load data for the ViewModel Items
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }
        }

        // Handle selection changed on LongListSelector
        private void MainLongListSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If selected item is null (no selection) do nothing
            if (MainLongListSelector.SelectedItem == null)
                return;

            // Navigate to the new page
            NavigationService.Navigate(new Uri("/DetailsPage.xaml?selectedItem=" + (MainLongListSelector.SelectedItem as ItemViewModel).ID, UriKind.Relative));

            // Reset selected item to null (no selection)
            MainLongListSelector.SelectedItem = null;
        }

        private void StartGPS_Click(object sender, RoutedEventArgs e)
        {
            if (checkingLocation)
            {
                subscription.Dispose();
                checkingLocation = false;
                StartButton.Content = "Start gps";
                InfoText.Text = "(geolocation off)";
            }
            else
            {
                //finder = new LocationFinder(true, GeoPositionAccuracy.High, 20);
                //finder.Subscribe(Observer.Create<CivicAddress>(address => UpdateAddressData(address)));
                subscription = finder.Subscribe(UpdateCoordinate);
               
                checkingLocation = true;
                StartButton.Content = "Stop gps";
            }
        }

        void UpdateCoordinate(GeoCoordinate geo)
        {
            InfoText.Text = geo.ToString();
            ICivicAddressResolver resolver = new CivicAddressResolver();
            var address = resolver.ResolveAddress(geo);
            InfoText.Text += address.City;
        }


        void UpdateAddressData(CivicAddress address)
        {
            // @TODO: Handle state like Cancelled or an error...
            if (!address.IsUnknown)
            {
                InfoText.Text =
                    address.AddressLine1 + "\r\n" + address.AddressLine2 + "\r\n" +
                    address.City;
            }
        }

        private void AccelButton_Click(object sender, RoutedEventArgs e)
        {
            var accel = Accelerometer.GetDefault();

            var accelReadings = 
                Observable.FromEventPattern<ReadingChangedHandler, AccelerometerReadingChangedEventArgs>
                (h => accel.ReadingChanged += h, h => accel.ReadingChanged -= h)
                .ObserveOnDispatcher()
                .Sample(TimeSpan.FromMilliseconds(200))
                .Select(x => x.EventArgs.Reading);
                //.ObserveOn(SynchronizationContext.Current);
                
            AccelButton.IsEnabled = false;
            accelReadings.Subscribe(
                Observer.Create<AccelerometerReading>(
                    next => 
                        Debug.WriteLine("X: {0}, Y: {1}, Z: {2}", next.AccelerationX, next.AccelerationY, next.AccelerationZ)));
        }
    }
}