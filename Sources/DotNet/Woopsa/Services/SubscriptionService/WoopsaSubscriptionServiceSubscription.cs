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
            DoPublish();
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
            if (monitorInterval != WoopsaSubscriptionServiceConst.MonitorIntervalLastPublishedValueOnly)
            {
                // create monitor timer
                _monitorTimer = channel.ServiceImplementation.TimerScheduler.AllocateTimer(monitorInterval);
                _monitorTimer.Elapsed += _monitorTimer_Elapsed;
                _monitorTimer.IsEnabled = true;
            }
            // Force immediate publishing of the current value
            DoMonitor();
            DoPublish();
        }

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

        protected override bool GetWatchedPropertyValue(out IWoopsaValue value)
        {
            try
            {
                if (_watchedProperty == null)
                {
                    var item = Root.ByPathOrNull(PropertyPath);
                    _watchedProperty = item as IWoopsaProperty;
                }
                if (_watchedProperty != null)
                {
                    value = _watchedProperty.Value;
                    return true;
                }
                else
                {
                    value = null;
                    return false;
                }
            }
            catch (Exception)
            {
                // The property might have become invalid, search it new the next time
                _watchedProperty = null;
                throw;
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
            bool isSingleNotification =
                monitorInterval == WoopsaSubscriptionServiceConst.MonitorIntervalLastPublishedValueOnly &&
                publishInterval == WoopsaSubscriptionServiceConst.PublishIntervalOnce;
            EventHandler<WoopsaNotificationEventArgs> handler;
            if (isSingleNotification)
                handler = 
                    (sender, e) => 
                    {
                        EnqueueNewMonitoredValue(e.Notification.Value);
                        DoPublish(); // there is not publish timer, force publishing the unique notification
                    };
            else
                handler = 
                    (sender, e) =>
                    {
                        EnqueueNewMonitoredValue(e.Notification.Value);
                    };
            _clientSubscription = subClient.Subscribe(relativePropertyPath, handler,
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

        protected override bool GetWatchedPropertyValue(out IWoopsaValue value)
        {
            try
            {
                value = ((WoopsaBaseClientObject)Root).Client.ClientProtocol.Read(PropertyPath);
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }

    }


}