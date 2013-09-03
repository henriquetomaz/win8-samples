using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Devices.Sensors;

using System.Reactive.Linq;
using System.Reactive;
using System.Threading;
using System.Reactive.Concurrency;

namespace SensorData
{
    using ReadingChangedHandler = TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs>;

    public static class Filter
    {
        public static IObservable<Vector> FindBigMovements(this IObservable<Vector> source)
        {
            // Use the Scan method to compare each element to the previous one
            var deltas =
                source.Scan(
                    new { last = new Vector(), delta = new Vector() },
                    (state, current) => {
                        var last = current;
                        var delta = new Vector(current.X - state.last.X, current.Y - state.last.Y, current.Z - state.last.Z);
                        return new { last, delta };
                    }
                );

            return deltas
                    .Where(d => d.delta.Length() > 1.0)
                    .Select(d => d.delta);
        }
    }

    #region Helper class
    public struct Vector
    {
        public Vector(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public readonly double X, Y, Z;

        public double Length()
        {
            return Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
        }
    }
    #endregion

    public class AccelerometerObservable
    {
        private readonly Accelerometer _accel;
        private readonly IObservable<Vector> _accelObs;
        readonly uint _reportInterval;
        const uint MIN_REPORT_INTERVAL = 16;

        private static Lazy<AccelerometerObservable> _instanceLazy = new Lazy<AccelerometerObservable>(() => new AccelerometerObservable());
        public static IObservable<Vector> Instance { get { return _instanceLazy.Value._accelObs; } }

        private Vector ToVector(AccelerometerReading r)
        {
            return new Vector((float)r.AccelerationX, (float)r.AccelerationY, (float)r.AccelerationZ);
        }

        private AccelerometerObservable()
        {
            _accel = Accelerometer.GetDefault();

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
                _reportInterval = _accel.MinimumReportInterval;
                if (_reportInterval < MIN_REPORT_INTERVAL)
                    _reportInterval = MIN_REPORT_INTERVAL;

                _accelObs =
                    Observable.FromEventPattern<ReadingChangedHandler, AccelerometerReadingChangedEventArgs>
                    (subscribeEvent, unsubscribeEvent)
                    .Select(x => ToVector(x.EventArgs.Reading))
                    .Publish()
                    .RefCount();
            }
            else {
                _accelObs = Observable.Empty<Vector>();
            }
        }
    }
}
