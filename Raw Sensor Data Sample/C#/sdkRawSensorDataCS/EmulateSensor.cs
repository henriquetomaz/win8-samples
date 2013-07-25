using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Concurrency;

namespace SensorData
{
    public static class EmulateSensor
    {
        private static Lazy<EventLoopScheduler> _scheduler = new Lazy<EventLoopScheduler>();

        private static IEnumerable<Vector> EmulateAccelerometerReading()
        {
            // Create a random number generator
            Random random = new Random();

            // Loop indefinitely
            for (double theta = 0; ; theta += .1) {
                // Generate a Vector3 in which the values of each axes slowly drift between -1 and 1 and
                Vector reading = new Vector((float)Math.Sin(theta), (float)Math.Cos(theta * 1.1), (float)Math.Sin(theta * .7));

                // At random intervals, generate a random spike in the data
                if (random.NextDouble() > .95) {
                    reading = new Vector((float)(random.NextDouble() * 3.0 - 1.5),
                     (float)(random.NextDouble() * 3.0 - 1.5),
                     (float)(random.NextDouble() * 3.0 - 1.5));

                }

                // return the vector and then sleep
                yield return reading;
                //Windows.System.Threading.Sleep(100);
                Task.Delay(50).Wait();
            }
        }

        public static IObservable<Vector> EmulateAccelerometer()
        {
            return EmulateAccelerometerReading().ToObservable(_scheduler.Value);
        }
    }
}
