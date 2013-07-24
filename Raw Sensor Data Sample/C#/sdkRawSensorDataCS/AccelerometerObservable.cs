using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Devices.Sensors;
using Microsoft.Xna.Framework;

using System.Reactive.Linq;
using System.Reactive;

namespace sdkRawSensorDataCS
{
    using ReadingChangedHandler = TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs>;

    public class AccelerometerObservable
    {
        private readonly Accelerometer _accel;
        private readonly IObservable<Vector3> _accelObs;
        readonly uint _reportInterval;
        const uint MIN_REPORT_INTERVAL = 16;

        private static Lazy<AccelerometerObservable> _instanceLazy = new Lazy<AccelerometerObservable>(() => new AccelerometerObservable());
        public static IObservable<Vector3> Instance { get { return _instanceLazy.Value._accelObs; } }

        private Vector3 ToVector(AccelerometerReading r)
        {
            return new Vector3( (float) r.AccelerationX, (float) r.AccelerationY, (float) r.AccelerationZ);
        }

        private AccelerometerObservable()
        {
            _accel = Accelerometer.GetDefault();

            _reportInterval = _accel.MinimumReportInterval;
            if (_reportInterval < MIN_REPORT_INTERVAL)
                _reportInterval = MIN_REPORT_INTERVAL;

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
                _accelObs =
                    Observable.FromEventPattern<ReadingChangedHandler, AccelerometerReadingChangedEventArgs>
                    (subscribeEvent, unsubscribeEvent)
                    //.Do(l => Debug.WriteLine("Publishing {0}", l.EventArgs.Reading.Timestamp)) //side effect to show it is running
                    .Select(x => ToVector(x.EventArgs.Reading))
                    .Publish()
                    .RefCount();
            }
            else {
                _accelObs = Observable.Empty<Vector3>();
            }
        }

        void StartReading()
        {
            _accel.ReportInterval = _reportInterval;
        }
    }
}
