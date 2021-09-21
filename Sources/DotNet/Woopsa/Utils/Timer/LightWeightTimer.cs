using System;

namespace Woopsa
{
    public class LightWeightTimer : IDisposable
    {
        #region Constructors

        internal LightWeightTimer(LightWeightTimerTimeClass timeClass,
            LightWeightTimerScheduler scheduler)
        {
            TimeClass = timeClass;
            Scheduler = scheduler;
        }

        #endregion

        #region Properties

        /// <summary>
        /// This value is false by default. You must set this value to
        /// true in order to start this LightWeightTimer.
        /// </summary>
        public bool IsEnabled { get; set; }

        public TimeSpan Interval => TimeClass.Interval;
        internal LightWeightTimerTimeClass TimeClass { get; }

        public LightWeightTimerScheduler Scheduler { get; }

        public event EventHandler<EventArgs> Elapsed;

        #endregion

        #region Protected

        protected virtual void OnElapsed()
        {
            if (Elapsed != null)
                Elapsed(this, new EventArgs());
        }

        #endregion

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

        #region Internal

        internal void Execute()
        {
            if (IsEnabled)
                OnElapsed();
        }

        #endregion
    }
}
