using System;
using System.Collections.Generic;

namespace Woopsa
{
    public abstract class BaseWoopsaSubscriptionServiceSubscription : IDisposable
    {
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
            _lock = new object();
            _notifications = new List<IWoopsaNotification>();
            _publishTimer = channel.ServiceImplementation.TimerScheduler.AllocateTimer(publishInterval);
            _publishTimer.Elapsed += _publishTimer_Elapsed;
            _publishTimer.IsEnabled = true;
        }

        public WoopsaSubscriptionChannel Channel { get; private set; }

        public WoopsaContainer Root { get; private set; }
        /// <summary>
        /// The auto-generated Id for this Subscription
        /// </summary>
        public int SubscriptionId { get; private set; }

        /// <summary>
        /// The Monitoring Interval is the interval at which this
        /// subscription checks the monitored value for changes.
        /// </summary>
        public TimeSpan MonitorInterval { get; private set; }

        /// <summary>
        /// The Publish Interval is the minimum time between each
        /// notification between the server and the client.
        /// </summary>
        public TimeSpan PublishInterval { get; private set; }

        public string PropertyPath { get; private set; }

        public virtual void Refresh()
        {
        }

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

        private void _publishTimer_Elapsed(object sender, EventArgs e)
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

        private IWoopsaValue _oldValue;
        private LightWeightTimer _publishTimer;
        private List<IWoopsaNotification> _notifications;
        private object _lock;
    }

    public abstract class WoopsaSubscriptionServiceSubscriptionMonitor :
        BaseWoopsaSubscriptionServiceSubscription
    {
        public WoopsaSubscriptionServiceSubscriptionMonitor(
                WoopsaSubscriptionChannel channel,
                WoopsaContainer root,
                int subscriptionId, string propertyPath,
                TimeSpan monitorInterval, TimeSpan publishInterval) :
            base(channel, root, subscriptionId, propertyPath, monitorInterval, publishInterval)
        {
            // create monitor timer
            _monitorTimer = channel.ServiceImplementation.TimerScheduler.AllocateTimer(monitorInterval);
            _monitorTimer.Elapsed += _monitorTimer_Elapsed;
            _monitorTimer.IsEnabled = true;
        }

        protected IWoopsaValue SynchronizedWatchedPropertyValue
        {
            get
            {
                Channel.OnBeforeWoopsaModelAccess();
                try
                {
                    return WatchedPropertyValue;
                }
                finally
                {
                    Channel.OnAfterWoopsaModelAccess();
                }
            }
        }

        protected abstract IWoopsaValue WatchedPropertyValue { get; }

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

        private void _monitorTimer_Elapsed(object sender, EventArgs e)
        {
            try
            {
                IWoopsaValue newValue = SynchronizedWatchedPropertyValue;
                EnqueueNewMonitoredValue(newValue);
            }
            catch (Exception)
            {
                // TODO : que faire avec un problème de monitoring ?
            }
        }

        private LightWeightTimer _monitorTimer;
    }

    public class WoopsaSubscriptionServiceSubscriptionMonitorServer :
        WoopsaSubscriptionServiceSubscriptionMonitor
    {
        public WoopsaSubscriptionServiceSubscriptionMonitorServer(
                WoopsaSubscriptionChannel channel,
                WoopsaContainer root,
                int subscriptionId, string propertyPath,
                TimeSpan monitorInterval, TimeSpan publishInterval) :
            base(channel, root, subscriptionId, propertyPath, monitorInterval, publishInterval)
        {
        }

        public override void Refresh()
        {
            _watchedProperty = null;
        }
        protected override IWoopsaValue WatchedPropertyValue
        {
            get
            {
                try
                {
                    if (_watchedProperty == null)
                    {
                        var item = Root.ByPath(PropertyPath);
                        if (item is IWoopsaProperty)
                            _watchedProperty = item as IWoopsaProperty;
                        else
                            throw new WoopsaNotFoundException(
                                string.Format("The path {0} does not reference a property",
                                PropertyPath));
                    }
                    return _watchedProperty.Value;
                }
                catch (Exception)
                {
                    // The property might have become invalid, search it new the next time
                    _watchedProperty = null;
                    throw;
                }
            }
        }

        #region IDisposable

        // ! Do not dispose the _watchedProperty as we are not the owner of this object

        #endregion

        private IWoopsaProperty _watchedProperty;
    }

    public class WoopsaSubscriptionServiceSubscriptionServerSubClient :
        BaseWoopsaSubscriptionServiceSubscription
    {
        public WoopsaSubscriptionServiceSubscriptionServerSubClient(
                WoopsaSubscriptionChannel channel,
                WoopsaContainer root,
                int subscriptionId, string propertyPath,
                TimeSpan monitorInterval, TimeSpan publishInterval,
                WoopsaBaseClientObject subClient, string relativePropertyPath) :
            base(channel, root, subscriptionId, propertyPath, monitorInterval, publishInterval)
        {
            _clientSubscription = subClient.Subscribe(relativePropertyPath,
                (sender, e) => { EnqueueNewMonitoredValue(e.Notification.Value); },
                monitorInterval, publishInterval);
        }

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (_clientSubscription != null)
                {
                    _clientSubscription.Dispose();
                    _clientSubscription = null;
                }
            }
        }

        #endregion

        private WoopsaClientSubscription _clientSubscription;
    }

    public class WoopsaSubscriptionServiceSubscriptionMonitorClient :
        WoopsaSubscriptionServiceSubscriptionMonitor
    {
        public WoopsaSubscriptionServiceSubscriptionMonitorClient(
                WoopsaSubscriptionChannel channel,
                WoopsaBaseClientObject root,
                int subscriptionId, string propertyPath,
                TimeSpan monitorInterval, TimeSpan publishInterval) :
            base(channel, root, subscriptionId, propertyPath, monitorInterval, publishInterval)
        {
        }

        protected override IWoopsaValue WatchedPropertyValue
        {
            get { return ((WoopsaBaseClientObject)Root).Client.ClientProtocol.Read(PropertyPath); }
        }

    }


}