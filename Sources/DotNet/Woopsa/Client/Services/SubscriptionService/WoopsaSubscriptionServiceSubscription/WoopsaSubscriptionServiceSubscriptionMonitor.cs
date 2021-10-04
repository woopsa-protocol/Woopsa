using System;

namespace Woopsa
{
    public abstract class WoopsaSubscriptionServiceSubscriptionMonitor :
        BaseWoopsaSubscriptionServiceSubscription
    {
        #region Constructor

        public WoopsaSubscriptionServiceSubscriptionMonitor(
        WoopsaSubscriptionChannel channel,
        WoopsaContainer root,
        int subscriptionId, string propertyPath,
        TimeSpan monitorInterval, TimeSpan publishInterval) :
            base(channel, root, subscriptionId, propertyPath, monitorInterval, publishInterval)
        {
            // Force immediate publishing of the current value
            DoMonitor();
            DoPublish();

            if (monitorInterval != WoopsaSubscriptionServiceConst.MonitorIntervalLastPublishedValueOnly)
            {
                // create monitor timer
                _monitorTimer = channel.ServiceImplementation.TimerScheduler.AllocateTimer(monitorInterval);
                _monitorTimer.Elapsed += _monitorTimer_Elapsed;
                _monitorTimer.IsEnabled = true;
            }
        }

        #endregion

        #region Fields / Attributes

        private LightWeightTimer _monitorTimer;

        #endregion

        #region Protected Methods

        protected bool GetSynchronizedWatchedPropertyValue(out IWoopsaValue value)
        {
            Channel.OnBeforeWoopsaModelAccess();
            try
            {
                return GetWatchedPropertyValue(out value);
            }
            finally
            {
                Channel.OnAfterWoopsaModelAccess();
            }
        }

        protected abstract bool GetWatchedPropertyValue(out IWoopsaValue value);

        protected override void DoPublish()
        {
            if (MonitorInterval == WoopsaSubscriptionServiceConst.MonitorIntervalLastPublishedValueOnly)
                DoMonitor();
            base.DoPublish();
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (_monitorTimer != null)
                {
                    _monitorTimer.Dispose();
                    _monitorTimer = null;
                }
            }
        }

        #endregion

        #region Private Methods

        private void _monitorTimer_Elapsed(object sender, EventArgs e)
        {
            DoMonitor();
        }

        private void DoMonitor()
        {
            try
            {
                IWoopsaValue newValue;
                if (GetSynchronizedWatchedPropertyValue(out newValue))
                    EnqueueNewMonitoredValue(newValue);
            }
            catch (Exception)
            {
                // TODO : que faire avec un problème de monitoring ?
            }
        }

        #endregion
    }
}