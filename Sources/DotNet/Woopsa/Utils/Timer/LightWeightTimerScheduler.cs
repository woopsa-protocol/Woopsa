using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Woopsa
{
    public sealed class LightWeightTimerScheduler : IDisposable
    {
        #region Constructors

        public LightWeightTimerScheduler()
        {
            _timeClasses = new List<LightWeightTimerTimeClass>();
            _timeClassesByTimeSpan = new Dictionary<TimeSpan, Woopsa.LightWeightTimerTimeClass>();
            _task = new Task(async () => await ExecuteAsync());
        }

        #endregion

        #region Properties

        public bool Terminated { get; private set; }

        #endregion

        #region Public Methods

        public void Start() => _task.Start();

        public LightWeightTimer AllocateTimer(TimeSpan interval)
        {
            lock (_timeClasses)
            {
                if (!_timeClassesByTimeSpan.TryGetValue(interval, out LightWeightTimerTimeClass timeClass))
                {
                    timeClass = new LightWeightTimerTimeClass(this, interval);
                    _timeClasses.Add(timeClass);
                    _timeClassesByTimeSpan[interval] = timeClass;
                }
                return timeClass.AllocateTimer();
            }
        }

        public void Terminate() => Terminated = true;

        #endregion

        #region Internal Methods

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

        #endregion

        #region Dispose

        public void Dispose()
        {
            Terminate();
            _task.Wait();
            _task?.Dispose();
            _task = null;
        }

        #endregion

        #region Events

        public event EventHandler Started;

        #endregion

        #region Private Methods

        private async Task ExecuteAsync()
        {
            if (Started != null)
                Started(this, new EventArgs());
            while (!Terminated)
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
                    await Task.Delay(1);
                }
                catch (Exception)
                {
                    // TODO : Manage exceptions properly
                }
            }
        }

        #endregion

        #region Fields / Attributes

        private Task _task;
        List<LightWeightTimerTimeClass> _timeClasses;
        Dictionary<TimeSpan, LightWeightTimerTimeClass> _timeClassesByTimeSpan;

        #endregion
    }
}
