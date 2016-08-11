using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using PairSubscriptionNotification = System.Collections.Generic.KeyValuePair<
    Woopsa.BaseWoopsaSubscriptionServiceSubscription, Woopsa.IWoopsaNotification>;

namespace Woopsa
{
    public class WoopsaSubscriptionChannel : IDisposable
    {
        #region static 
        static WoopsaSubscriptionChannel()
        {
            Random random = new Random();
            _lastChannelId = random.Next();
            _lock = new object();
        }

        private static int GetNextChannelId()
        {
            lock(_lock)
            {
                _lastChannelId++;
                return _lastChannelId;
            }
        }

        private static object _lock;
        private static int _lastChannelId;

        #endregion

        internal WoopsaSubscriptionChannel(WoopsaSubscriptionServiceImplementation serviceImplementation,
            int notificationQueueSize)
        {
            _subscriptions = new Dictionary<int, BaseWoopsaSubscriptionServiceSubscription>();
            _waitNotificationEvent = new AutoResetEvent(false);
            _waitStopEvent = new ManualResetEvent(false);
            ServiceImplementation = serviceImplementation;
            _idLock = new Object();
            Id = GetNextChannelId();
            NotificationQueueSize = notificationQueueSize;
            _pendingNotifications = new NotificationConcurrentQueue(notificationQueueSize);
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

        /// <summary>
        /// This event is triggered before Channel accesses the WoopsaObject model.
        /// It is usefull to protect against concurrent WoopsaObject accesses at a global level.
        /// </summary>
        public event EventHandler BeforeWoopsaModelAccess;

        /// <summary>
        /// This event is triggered after Channel accesses the WoopsaObject model.
        /// It is guaranteed that for each BeforeWoopsaModelAccess event fired, 
        /// the AfterWoopsaModelAccess event will be fired.
        /// </summary>
        public event EventHandler AfterWoopsaModelAccess;

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

        internal void SubscriptionPublishNotifications(
            BaseWoopsaSubscriptionServiceSubscription subscription,
            List<IWoopsaNotification> notifications)
        {
            foreach (var notification in notifications)
            {
                bool itemDiscarded;

                _lastNotificationId++;
                if (_lastNotificationId >= WoopsaSubscriptionServiceConst.MaximumNotificationId)
                    _lastNotificationId = WoopsaSubscriptionServiceConst.MinimumNotificationId;
                notification.Id = _lastNotificationId;
                _pendingNotifications.Enqueue(subscription, notification, out itemDiscarded);
                if (itemDiscarded)
                    _notificationsLost = true;
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

        public void Stop()
        {
            _waitStopEvent.Set();
        }
        public const int IdResetLostNotification = 0;

        internal protected virtual void OnBeforeWoopsaModelAccess()
        {
            if (BeforeWoopsaModelAccess != null)
                BeforeWoopsaModelAccess(this, new EventArgs());
        }

        internal protected virtual void OnAfterWoopsaModelAccess()
        {
            if (AfterWoopsaModelAccess != null)
                AfterWoopsaModelAccess(this, new EventArgs());
        }

        #region IDisposable

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

        #endregion

        public IWoopsaNotifications WaitNotification(TimeSpan timeout, int lastNotificationId)
        {
            WoopsaServerNotifications result = new WoopsaServerNotifications();

            _watchClientActivity.Restart();
            if (lastNotificationId != IdResetLostNotification)
            {
                if (!_notificationsLost)
                {
                    _lastRemovedNotificationId = _pendingNotifications.RemoveOlder(_lastRemovedNotificationId, lastNotificationId);
                    // Wait notifications if none is available
                    if (_pendingNotifications.Count == 0)
                    {
                        _waitNotificationEvent.Reset();
                        WaitHandle.WaitAny(new WaitHandle[] { _waitStopEvent, _waitNotificationEvent }, timeout);
                    }
                    // prepare result with all available notifications without dequeueing
                    result.AddRange(_pendingNotifications.PeekNotifications());
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
                result.AddRange(_pendingNotifications.PeekNotifications());
            }
            _watchClientActivity.Restart();
            return result;
        }

        private bool FindWoopsaClientAlongPath(WoopsaContainer root, string path,
            out WoopsaBaseClientObject client, out string relativePath)
        {
            OnBeforeWoopsaModelAccess();
            try
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
            finally
            {
                OnAfterWoopsaModelAccess();
            }
        }

        internal WoopsaSubscriptionServiceImplementation ServiceImplementation { get; private set; }

        // Instance

        private bool _notificationsLost;
        private int _lastSubscriptionId;
        private int _lastNotificationId;
        private int _lastRemovedNotificationId;

        private Dictionary<int, BaseWoopsaSubscriptionServiceSubscription> _subscriptions;
        private NotificationConcurrentQueue _pendingNotifications;

        private AutoResetEvent _waitNotificationEvent;
        private ManualResetEvent _waitStopEvent;

        private object _idLock;

        private Stopwatch _watchClientActivity;
    }

    internal class NotificationConcurrentQueue
    {
        public NotificationConcurrentQueue(int maxQueueSize)
        {
            _notifications = new LinkedList<PairSubscriptionNotification>();
            MaxQueueSize = maxQueueSize;
        }

        public int MaxQueueSize { get; private set; }

        public int Count
        {
            get
            {
                lock (_notifications)
                    return _notifications.Count;
            }
        }

        public void Enqueue(BaseWoopsaSubscriptionServiceSubscription subscription, IWoopsaNotification notification,
            out bool itemDiscarded)
        {
            PairSubscriptionNotification pair = new PairSubscriptionNotification(subscription, notification);
            lock (_notifications)
            {
                if (subscription.MonitorInterval == WoopsaSubscriptionServiceConst.MonitorIntervalLastPublishedValueOnly)
                {
                    // remove any existing notification from this subscription, as we want to keep only the last
                    LinkedListNode<PairSubscriptionNotification> node = _notifications.First;
                    while (node != null)
                    {
                        var next = node.Next;
                        if (node.Value.Key == subscription)
                            _notifications.Remove(node);
                        node = next;
                    }
                }
                itemDiscarded = _notifications.Count >= MaxQueueSize;
                if (itemDiscarded)
                    while (_notifications.Count >= MaxQueueSize)
                        _notifications.RemoveFirst();
                _notifications.AddLast(pair);
            }
        }
        public IWoopsaNotification[] PeekNotifications()
        {
            lock (_notifications)
            {
                IWoopsaNotification[] result = new IWoopsaNotification[_notifications.Count];
                LinkedListNode<PairSubscriptionNotification> node = _notifications.First;
                int i = 0;
                while (node != null)
                {
                    result[i] = node.Value.Value;
                    i++;
                    node = node.Next;
                }
                return result;
            }
        }

        public int RemoveOlder(int notificationIdAgeOrigin, int notificationIdToRemoveUpTo)
        {
            int newAgeOrigin = notificationIdAgeOrigin;
            lock (_notifications)
            {
                LinkedListNode<PairSubscriptionNotification> node = _notifications.First;
                while (node != null)
                {
                    var next = node.Next;
                    if (notificationAge(notificationIdAgeOrigin, node.Value.Value.Id) <=
                        notificationAge(notificationIdAgeOrigin, notificationIdToRemoveUpTo))
                    {
                        _notifications.Remove(node);
                        newAgeOrigin = node.Value.Value.Id;
                    }
                    else
                        break;
                    node = next;
                }
            }
            return newAgeOrigin;
        }

        private int notificationAge(int notificationIdOrigin, int notificationId)
        {
            int age = notificationId - notificationIdOrigin;
            if (age < 0)
                age += WoopsaSubscriptionServiceConst.MaximumNotificationId + age; // Note : Id restart from 1
            return age;
        }

        private LinkedList<PairSubscriptionNotification> _notifications;
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
