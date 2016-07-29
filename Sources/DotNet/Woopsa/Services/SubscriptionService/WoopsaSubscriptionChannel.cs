using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Woopsa
{
    public class WoopsaSubscriptionChannel : IDisposable
    {
        static WoopsaSubscriptionChannel()
        {
            _random = new Random();
            _lastChannelId = _random.Next();
        }

        public WoopsaSubscriptionChannel(int notificationQueueSize)
        {
            _subscriptions = new Dictionary<int, BaseWoopsaSubscriptionServiceSubscription>();
            _waitNotificationEvent = new AutoResetEvent(false);
            _waitStopEvent = new ManualResetEvent(false);
            _idLock = new Object();
                lock (_idLock)
            {
                _lastChannelId++;
                Id = _lastChannelId;
            }
            NotificationQueueSize = notificationQueueSize;
            _pendingNotifications = new ConcurrentQueue<IWoopsaNotification>();
            _watchClientActivity = new Stopwatch();
            _watchClientActivity.Start();

        }

        /// <summary>
        /// The auto-generated Id for this Subscription Channel
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// The maximum size of the queue of arriving notifications.
        /// This means that if events come faster than they can be sent,
        /// old events will be "forgotten"
        /// </summary>
        public int NotificationQueueSize { get; private set; }

        public bool ClientTimedOut
        {
            get { return _watchClientActivity.Elapsed > WoopsaSubscriptionServiceConst.SubscriptionChannelLifeTime; }
        }

        public int RegisterSubscription(WoopsaContainer root, bool isServerSide, 
            string woopsaPropertyPath, TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            BaseWoopsaSubscriptionServiceSubscription newSubscription;
            int subscriptionId;
            _watchClientActivity.Restart();
            lock (_idLock)
            {
                _lastSubscriptionId++;
                subscriptionId = _lastSubscriptionId;
            }
            if (isServerSide)
            {
                WoopsaBaseClientObject subclient;
                string relativePath;
                if (FindWoopsaClientAlongPath(root, woopsaPropertyPath, out subclient, out relativePath))
                    // subscribe directly instead of polling
                    newSubscription = new WoopsaSubscriptionServiceSubscriptionServerSubClient(
                        this, root, subscriptionId, woopsaPropertyPath, monitorInterval, publishInterval,
                        subclient, relativePath);
                else
                    newSubscription = new WoopsaSubscriptionServiceSubscriptionMonitorServer(
                        this, root, subscriptionId, woopsaPropertyPath, monitorInterval, publishInterval);
            }
            else
                newSubscription = new WoopsaSubscriptionServiceSubscriptionMonitorClient(
                    this, (WoopsaBaseClientObject)root, subscriptionId, woopsaPropertyPath, monitorInterval, publishInterval);
            lock (_subscriptions)
                _subscriptions.Add(newSubscription.SubscriptionId, newSubscription);
            return newSubscription.SubscriptionId;
        }

        internal void SubscriptionPublishNotifications(List<IWoopsaNotification> notifications)
        {
            foreach (var notification in notifications)
            {
                if (_pendingNotifications.Count >= NotificationQueueSize)
                {
                    // If the queue is full, raise the notificationsLost flag
                    // and remove the oldest notification in the queue.
                    // The WaitNotification method will then throw an exception
                    // until the client has acknowledged the loss of notifications
                    _notificationsLost = true;
                    IWoopsaNotification discardedNotification;
                    _pendingNotifications.TryDequeue(out discardedNotification);
                }
                _lastNotificationId++;
                if (_lastNotificationId >= WoopsaSubscriptionServiceConst.MaximumNotificationId)
                    _lastNotificationId = WoopsaSubscriptionServiceConst.MinimumNotificationId;
                notification.Id = _lastNotificationId;
                _pendingNotifications.Enqueue(notification);
            }
            _waitNotificationEvent.Set();
        }

        public bool UnregisterSubscription(int subscriptionId)
        {
            BaseWoopsaSubscriptionServiceSubscription subscription;
            _watchClientActivity.Restart();
            lock (_subscriptions)
            {
                if (_subscriptions.ContainsKey(subscriptionId))
                    subscription = _subscriptions[subscriptionId];
                else
                    subscription = null;
            }
            if (subscription != null)
            {
                subscription.Dispose();
                lock (_subscriptions)
                    _subscriptions.Remove(subscriptionId);
                return true;
            }
            else
                return false;
        }

        internal void Refresh()
        {
            foreach (var item in _subscriptions.Values)
                item.Refresh();
        }

        public void Stop()
        {
            _waitStopEvent.Set();
        }
        public const int IdResetLostNotification = 0;

        public IWoopsaNotifications WaitNotification(TimeSpan timeout, int lastNotificationId)
        {
            WoopsaServerNotifications result = new WoopsaServerNotifications();

            _watchClientActivity.Restart();
            if (lastNotificationId != IdResetLostNotification)
            {
                IWoopsaNotification notification;
                if (!_notificationsLost)
                {
                    // Remove acknowledged notifications already sent to the client
                    while (_pendingNotifications.Count > 0)
                    {
                        if (_pendingNotifications.TryPeek(out notification))
                        {
                            if (notificationAge(notification.Id) <= notificationAge(lastNotificationId))
                            {
                                if (_pendingNotifications.TryDequeue(out notification))
                                    _lastRemovedNotificationId = notification.Id;
                            }
                            else
                                break;
                        }
                    }
                    // Wait notifications if none is available
                    if (_pendingNotifications.Count == 0)
                    {
                        _waitNotificationEvent.Reset();
                        WaitHandle.WaitAny(new WaitHandle[] { _waitStopEvent, _waitNotificationEvent }, timeout);
                    }
                    // prepare result with all available notifications
                    while (_pendingNotifications.TryDequeue(out notification))
                        result.Add(notification);
                }
                else
                    throw new WoopsaNotificationsLostException("Notifications have been lost because the queue was full. Acknowledge the error by calling WaitNotification with LastNotificationId = 0");
            }
            else
            {
                _notificationsLost = false;
                // Wait notifications if none is available
                if (_pendingNotifications.Count == 0)
                {
                    _waitNotificationEvent.Reset();
                    WaitHandle.WaitAny(new WaitHandle[] { _waitStopEvent, _waitNotificationEvent }, timeout);
                }
                // prepare result with all available notifications without dequeueing
                IWoopsaNotification[] notifications = _pendingNotifications.ToArray();
                foreach (var item in notifications)
                    result.Add(item);
            }
            _watchClientActivity.Restart();
            return result;
        }

        private int notificationAge(int notificationId)
        {
            // _lastRemovedNotificationId used as a moving age reference
            int age = notificationId - _lastRemovedNotificationId;
            if (age < 0) // Id restart from 1
                age += WoopsaSubscriptionServiceConst.MaximumNotificationId + age;
            return age;
        }

        private bool FindWoopsaClientAlongPath(WoopsaContainer root, string path,
            out WoopsaBaseClientObject client, out string relativePath)
        {
            string[] pathParts = path.Split(WoopsaConst.WoopsaPathSeparator);
            WoopsaContainer container = root;
            bool found = false;

            client = null;
            relativePath = string.Empty;
            for (int i = 0; i < pathParts.Length; i++)
            {
                if (container is WoopsaBaseClientObject)
                {
                    client = (WoopsaBaseClientObject)container;
                    for (int j = i; j < pathParts.Length; j++)
                        relativePath += WoopsaConst.WoopsaPathSeparator + pathParts[j];
                    found = true;
                    break;
                }
                else if (container == null)
                    break;
                else if (!string.IsNullOrEmpty(pathParts[i]))
                    container = container.ByNameOrNull(pathParts[i]) as WoopsaContainer;
            }
            return found;
        }

        // Static

        private static Random _random;
        private static int _lastChannelId;

        // Instance

        private bool _notificationsLost;
        private int _lastSubscriptionId;
        private int _lastNotificationId;
        private int _lastRemovedNotificationId;

        private Dictionary<int, BaseWoopsaSubscriptionServiceSubscription> _subscriptions;
        private ConcurrentQueue<IWoopsaNotification> _pendingNotifications;

        private AutoResetEvent _waitNotificationEvent;
        private ManualResetEvent _waitStopEvent;

        private object _idLock;

        private Stopwatch _watchClientActivity;


        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();
                foreach (var item in _subscriptions.Values)
                    item.Dispose();
                _waitNotificationEvent.Dispose();
                _waitStopEvent.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public class NotificationEventArgs : EventArgs
    {
        public NotificationEventArgs(WoopsaServerNotification notification)
        {
            Notification = notification;
        }

        public WoopsaServerNotification Notification { get; private set; }
    }
}
