using System;
using System.Collections.Generic;

namespace Woopsa
{
    public abstract class BaseWoopsaSubscriptionServiceSubscription : IDisposable
    {
        #region Constructor

        protected BaseWoopsaSubscriptionServiceSubscription(
            WoopsaSubscriptionChannel channel,
            WoopsaContainer root,
            int subscriptionId, string propertyPath,
            TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            Channel = channel;
            Root = root;
            SubscriptionId = subscriptionId;
            MonitorInterval = monitorInterval;
            PublishInterval = publishInterval;
            PropertyPath = propertyPath;
            IncrementalObjectId = WoopsaUtils.NextIncrementalObjectId();
            _lock = new object();
            _notifications = new List<IWoopsaNotification>();
            if (monitorInterval == WoopsaSubscriptionServiceConst.MonitorIntervalLastPublishedValueOnly &&
                publishInterval == WoopsaSubscriptionServiceConst.PublishIntervalOnce)
                DoPublish();
            else if (publishInterval > TimeSpan.FromMilliseconds(0))
            {
                _publishTimer = channel.ServiceImplementation.TimerScheduler.AllocateTimer(publishInterval);
                _publishTimer.Elapsed += _publishTimer_Elapsed;
                _publishTimer.IsEnabled = true;
            }
            else
                throw new WoopsaException("A publish interval of 0 with a non-zero monitor interval is not allowed");
        }

        #endregion

        #region Fields / Attributes

        private IWoopsaValue _oldValue;
        private LightWeightTimer _publishTimer;
        private List<IWoopsaNotification> _notifications;
        private object _lock;

        #endregion

        #region Properties

        public WoopsaSubscriptionChannel Channel { get; }

        public WoopsaContainer Root { get; }
        /// <summary>
        /// The auto-generated Id for this Subscription
        /// </summary>
        public int SubscriptionId { get; }

        /// <summary>
        /// The Monitoring Interval is the interval at which this
        /// subscription checks the monitored value for changes.
        /// </summary>
        public TimeSpan MonitorInterval { get; }

        /// <summary>
        /// The Publish Interval is the minimum time between each
        /// notification between the server and the client.
        /// </summary>
        public TimeSpan PublishInterval { get; }

        public string PropertyPath { get; }

        /// <summary>
        /// A unique number to identifiy internally this object.
        /// </summary>
        public ulong IncrementalObjectId { get; }

        #endregion

        #region Protected Methods

        protected void EnqueueNewMonitoredValue(IWoopsaValue newValue)
        {
            if (!newValue.IsSameValue(_oldValue))
            {
                _oldValue = newValue;
                if (newValue.TimeStamp == null)
                    newValue = WoopsaValue.CreateUnchecked(newValue.AsText, newValue.Type, DateTime.Now);
                WoopsaServerNotification newNotification = new WoopsaServerNotification(newValue, SubscriptionId);
                lock (_lock)
                {
                    if (MonitorInterval == WoopsaSubscriptionServiceConst.MonitorIntervalLastPublishedValueOnly)
                        _notifications.Clear();
                    _notifications.Add(newNotification);
                }
            }
        }

        protected virtual void DoPublish()
        {
            List<IWoopsaNotification> notificationsList;
            lock (_lock)
            {
                if (_notifications.Count > 0)
                {
                    notificationsList = _notifications;
                    _notifications = new List<IWoopsaNotification>();
                }
                else
                    notificationsList = null;
            }
            if (notificationsList != null)
                Channel.SubscriptionPublishNotifications(this, notificationsList);
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_publishTimer != null)
                {
                    _publishTimer.Dispose();
                    _publishTimer = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private Methods

        private void _publishTimer_Elapsed(object sender, EventArgs e)
        {
            DoPublish();
        }

        #endregion
    }
}