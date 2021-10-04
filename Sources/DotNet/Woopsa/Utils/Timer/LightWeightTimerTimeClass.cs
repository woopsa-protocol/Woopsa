using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Woopsa
{
    internal class LightWeightTimerTimeClass
    {
        #region Constructors

        public LightWeightTimerTimeClass(LightWeightTimerScheduler scheduler, TimeSpan timeClassInterval)
        {
            _timers = new List<LightWeightTimer>();
            _watch = new Stopwatch();
            _watch.Start();
            _scheduler = scheduler;
            Interval = timeClassInterval;
        }

        #endregion

        #region Properties

        public TimeSpan Interval { get; }

        public int Count => _timers.Count;

        #endregion

        #region Internal methods

        internal void Execute()
        {
            if (_watch.Elapsed >= Interval)
            {
                _watch.Restart();
                int i = 0;

                LightWeightTimer timer = null;
                do
                {
                    lock (_timers)
                    {
                        if (i < _timers.Count)
                            timer = _timers[i];
                        else
                            timer = null;
                        i++;
                    }
                    if (timer != null)
                        try
                        {
                            timer.Execute();
                        }
                        catch (Exception)
                        {
                            // TODO : Define how to manage properly user code exceptions
                        }
                }
                while (timer != null);
            }
        }

        #endregion

        #region Public methods

        public LightWeightTimer AllocateTimer()
        {
            LightWeightTimer newTimer = new LightWeightTimer(this, _scheduler);
            lock (_timers)
                _timers.Add(newTimer);
            return newTimer;
        }

        public void DeallocateTimer(LightWeightTimer timer)
        {
            lock (_timers)
                _timers.Remove(timer);
        }

        #endregion

        #region Fields / Attributes


        private List<LightWeightTimer> _timers;
        private Stopwatch _watch;
        private LightWeightTimerScheduler _scheduler;

        #endregion
    }
}
