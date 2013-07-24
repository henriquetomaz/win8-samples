using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Device.Location;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;

namespace MetalWrench.VegasLocater
{
    public class LocationFinder : IObservable<CivicAddress>
    {
        private IObservable<GeoCoordinate> _geoCoordinates;
        private ICivicAddressResolver _resolver;
        private IDisposable _disposable;

        public LocationFinder(bool includeAllCities = true, GeoPositionAccuracy accuracy = GeoPositionAccuracy.High, double movementThreshold = 5.0)
        {
            _geoCoordinates = new GeoCoordinateObservable(accuracy, movementThreshold);

            // There is no usefully-implemented CivicAddressResolver in Windows Phone 7.1.  Maybe Windows 8?
            _resolver = new VegasResolver(includeAllCities);

            // Testing code for Rx - make darn sure I have a reference to something in System.Reactive.Linq.
            new[] { 1, 2, 3 }.ToObservable();
        }

        #region Location Finder

        public void Stop()
        {
            /*
            if (_watcher != null)
            {
                _watcher.Stop();
                _watcher = null;
            }
             */
            if (_disposable != null)
            {
                _disposable.Dispose();
                _disposable = null;
            }
        }

        #endregion Location Finder

        public IDisposable Subscribe(IObserver<CivicAddress> observer)
        {
            _disposable = _geoCoordinates.Subscribe(coord => observer.OnNext(_resolver.ResolveAddress(coord)));
            return _disposable;
        }
    }
}
