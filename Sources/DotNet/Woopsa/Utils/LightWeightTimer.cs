using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Woopsa
{
    public sealed class LightWeightTimerScheduler : IDisposable
    {
        public LightWeightTimerScheduler()
        {
            _timeClasses = new List<LightWeightTimerTimeClass>();
            _timeClassesByTimeSpan = new Dictionary<TimeSpan, Woopsa.LightWeightTimerTimeClass>();
            _thread = new Thread(Execute);
            _thread.Name = "Woopsa_LightWeightTimers";
            _thread.IsBackground = true;
        }

        public void Start()
        {
            _thread.Start();
        }

        public LightWeightTimer AllocateTimer(TimeSpan interval)
        {
            lock (_timeClasses)
            {
                LightWeightTimerTimeClass timeClass;
                if (!_timeClassesByTimeSpan.TryGetValue(interval, out timeClass))
                {
                    timeClass = new LightWeightTimerTimeClass(this, interval);
                    _timeClasses.Add(timeClass);
                    _timeClassesByTimeSpan[interval] = timeClass;
                }
                return timeClass.AllocateTimer();
            }
        }

        internal void DeallocateTimer(LightWeightTimer timer, LightWeightTimerTimeClass timeClass)
        {
            lock (_timeClasses)
            {
                timeClass.DeallocateTimer(timer);
                if (timeClass.Count == 0)
                {
                    _timeClasses.Remove(timeClass);
                    _timeClassesByTimeSpan.Remove(timeClass.Interval);
                }
            }
        }

        public void Terminate()
        {
            _terminated = true;
        }

        public bool Terminated { get { return _terminated; } }

        public void Dispose()
        {
            Terminate();
            _thread.Join();
        }

        public event EventHandler Started;

        private void Execute(object obj)
        {
            if (Started != null)
                Started(this, new EventArgs());
            while (!_terminated)
            {
                try
                {
                    int i = 0;
                    LightWeightTimerTimeClass timeClass = null;

                    do
                    {
                        lock (_timeClasses)
                        {
                            if (i < _timeClasses.Count)
                                timeClass = _timeClasses[i];
                            else
                                timeClass = null;
                            i++;
                        }
                        if (timeClass != null)
                            timeClass.Execute();
                    }
                    while (timeClass != null);
                    Thread.Sleep(1);
                }
                catch (Exception)
                {
                    // TODO : Manage exceptions properly
                }
            }
        }

        private Thread _thread;
        List<LightWeightTimerTimeClass> _timeClasses;
        Dictionary<TimeSpan, LightWeightTimerTimeClass> _timeClassesByTimeSpan;
        private bool _terminated;

    }

    internal class LightWeightTimerTimeClass
    {
        public LightWeightTimerTimeClass(LightWeightTimerScheduler scheduler, TimeSpan timeClassInterval)
        {
            _timers = new List<LightWeightTimer>();
            _watch = new Stopwatch();
            _watch.Start();
            _scheduler = scheduler;
            Interval = timeClassInterval;
        }

        public TimeSpan Interval { get; private set; }

        public int Count { get { return _timers.Count; } }

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

        private List<LightWeightTimer> _timers;
        private Stopwatch _watch;
        private LightWeightTimerScheduler _scheduler;
    }

    public class LightWeightTimer : IDisposable
    {
        internal LightWeightTimer(LightWeightTimerTimeClass timeClass,
            LightWeightTimerScheduler scheduler)
        {
            TimeClass = timeClass;
            Scheduler = scheduler;
        }

        /// <summary>
        /// This value is false by default. You must set this value to
        /// true in order to start this LightWeightTimer.
        /// </summary>
        public bool IsEnabled { get; set; }

        public TimeSpan Interval { get { return TimeClass.Interval; } }
        internal LightWeightTimerTimeClass TimeClass { get; private set; }

        public LightWeightTimerScheduler Scheduler { get; private set; }

        public event EventHandler<EventArgs> Elapsed;

        protected virtual void OnElapsed()
        {
            if (Elapsed != null)
                Elapsed(this, new EventArgs());
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Scheduler.DeallocateTimer(this, TimeClass);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable

        internal void Execute()
        {
            if (IsEnabled)
                OnElapsed();
        }

    }
}
