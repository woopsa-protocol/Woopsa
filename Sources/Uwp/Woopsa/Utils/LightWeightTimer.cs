using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Woopsa
{
    public class LightWeightTimer : IDisposable
    {
        #region Statics

        static LightWeightTimer()
        {
            _timers = new List<LightWeightTimer>();
        }
        private static List<LightWeightTimer> _timers;

        #endregion

        public LightWeightTimer(TimeSpan interval)
        {
            Interval = interval;
            lock (_timers)
                _timers.Add(this);
        }
        public LightWeightTimer(int interval) : this(TimeSpan.FromMilliseconds(interval))
        {
        }

        /// <summary>
        /// The interval at which to trigger the callbac
        /// </summary>
        public TimeSpan Interval { get; private set; }

        /// <summary>
        /// This value is false by default. You must set this value to
        /// true in order to start this LightWeightTimer.
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                return _watch.IsRunning;
            }
            set
            {
                if (value)
                    _watch.Start();
                else
                    _watch.Stop();
            }
        }

        public event EventHandler<EventArgs> Elapsed;
        private Stopwatch _watch = new Stopwatch();

        private static void Execute(object obj)
        {
            while (true)
            {
                var i = 0;
                LightWeightTimer timer = null;
                for (;;)
                {
                    lock (_timers)
                    {
                        if (i < _timers.Count)
                            timer = _timers[i++];
                        else
                            break;
                    }
                    if (timer.IsEnabled && timer._watch.Elapsed >= timer.Interval)
                    {
                        timer._watch.Restart();
                        timer.OnElapsed();
                    }
                }
            }
        }

        protected virtual void OnElapsed()
        {
            if (Elapsed != null)
                Elapsed(this, new EventArgs());
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_timers)
                {
                    _timers.Remove(this);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
