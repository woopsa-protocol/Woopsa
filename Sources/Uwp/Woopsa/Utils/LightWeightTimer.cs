using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Woopsa
{
    public class LightWeightTimer : IDisposable
    {
        public LightWeightTimer(int interval)
        {
            Interval = interval;
            lock (_timers)
            {
                _timers.Add(this);
            }
        }

        static LightWeightTimer()
        {
            //_thread = new Thread(Execute);
            //_thread.Name = "Thread_LightWeightTimers";
            //_thread.IsBackground = true;
            //_thread.Start();
        }

        /// <summary>
        /// The interval at which to repeat the callback, in milliseconds
        /// </summary>
        public int Interval { get; private set; }

        /// <summary>
        /// This value is false by default. You must set this value to
        /// true in order to start this LightWeightTimer.
        /// </summary>
        public bool Enabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                if (value)
                {
                    _watch.Start();
                }
                _isEnabled = value;
            }
        }
        private bool _isEnabled = false;

        public event EventHandler<EventArgs> Elapsed;

        private Stopwatch _watch = new Stopwatch();

        //private static Thread _thread;
        private static List<LightWeightTimer> _timers = new List<LightWeightTimer>();
        private static object _lock = new object();

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
                    if (timer.Enabled && timer._watch.ElapsedMilliseconds >= timer.Interval)
                    {
                        timer.OnElapsed();
                        timer._watch.Restart();
                    }
                }
                //Thread.Sleep(1);
            }
        }

        protected virtual void OnElapsed()
        {
            if (Elapsed != null)
            {
                Elapsed(this, new EventArgs());
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_timers)
                {
                    _timers.Remove(this);
                }
                if (_timers.Count == 0)
                {
                    //_thread = null;
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
