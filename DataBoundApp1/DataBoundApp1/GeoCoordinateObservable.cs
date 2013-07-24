using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetalWrench.VegasLocater
{
    // A GeoCoordinateWatcher provides two streams of events - first, GeoCoordinates, and second, GeoPositionStatusChangedEventArgs.
    // @TODO: Can I replace this with the following?
    //  GeoCoordinateWatcher watcher = new GeoCoordinateWatcher(accuracy);
    //  watcher.MovementThreshold = movementThreshold;
    //  Observable.Create(observer => { _watcher.Start();  _watcher.PositionChanged += (o, e) => observer.OnNext(e.Position.Location);  return new Disposer(watcher); })
    public class GeoCoordinateObservable : IObservable<GeoCoordinate>
    {
        private GeoCoordinateWatcher _watcher;

        public GeoCoordinateObservable(GeoPositionAccuracy accuracy, double movementThreshold)
        {
            _watcher = new GeoCoordinateWatcher(accuracy);
            _watcher.MovementThreshold = movementThreshold;
        }

        public IDisposable Subscribe(IObserver<GeoCoordinate> observer)
        {
            _watcher.Start();

            _watcher.PositionChanged += (o, e) =>
            {
                observer.OnNext(e.Position.Location);
            };

            return new Disposer(this, observer);
        }

        private class Disposer : IDisposable
        {
            GeoCoordinateObservable _observable;
            IObserver<GeoCoordinate> _observer;

            internal Disposer(GeoCoordinateObservable observable, IObserver<GeoCoordinate> observer)
            {
                _observable = observable;
                _observer = observer;
            }

            public void Dispose()
            {
                // Very important - stop the GeoCoordinateWatcher so we don't waste power.
                _observable._watcher.Stop();
                _observer.OnCompleted();
            }
        }
    }
}
