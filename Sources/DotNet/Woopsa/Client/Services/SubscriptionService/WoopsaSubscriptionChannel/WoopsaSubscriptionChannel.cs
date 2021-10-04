using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

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
            lock (_lock)
            {
                _lastChannelId++;
                return _lastChannelId;
            }
        }

        private static object _lock;
        private static int _lastChannelId;

        #endregion

        #region Constructor

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

        #endregion

        #region Fields / Attributes

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

        #endregion

        #region Properties

        /// <summary>
        /// The auto-generated Id for this Subscription Channel
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// The maximum size of the queue of arriving notifications.
        /// This means that if events come faster than they can be sent,
        /// old events will be "forgotten"
        /// </summary>
        public int NotificationQueueSize { get; }

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

        public bool ClientTimedOut => _watchClientActivity.Elapsed > WoopsaSubscriptionServiceConst.SubscriptionChannelLifeTime;

        internal WoopsaSubscriptionServiceImplementation ServiceImplementation { get; }

        #endregion

        #region Constants

        public const int IdResetLostNotification = 0;

        #endregion

        #region Public Methods

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
                    result.AddRange(_pendingNotifications.PeekNotifications(WoopsaSubscriptionServiceConst.MaxGroupedNotificationCount));
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
                result.AddRange(_pendingNotifications.PeekNotifications(WoopsaSubscriptionServiceConst.MaxGroupedNotificationCount));
            }
            _watchClientActivity.Restart();
            return result;
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
                if (FindWoopsaClientAlongPath(root, woopsaPropertyPath,
                    out WoopsaBaseClientObject subclient,
                    out string relativePath))
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

        public bool UnregisterSubscription(int subscriptionId)
        {
            bool result;
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
                result = true;
            }
            else
                result = false;
            _pendingNotifications.RemoveNotificationsForSubscription(subscriptionId);
            return result;
        }

        public void Stop()
        {
            _waitStopEvent.Set();
        }

        #endregion

        #region Internal Methods

        internal bool OnCanWatch(BaseWoopsaSubscriptionServiceSubscription subscription, IWoopsaProperty itemProperty)
        {
            return ServiceImplementation.OnCanWatch(subscription, itemProperty);
        }

        internal void SubscriptionPublishNotifications(
            BaseWoopsaSubscriptionServiceSubscription subscription,
            List<IWoopsaNotification> notifications)
        {
            foreach (var notification in notifications)
            {
                _lastNotificationId++;
                if (_lastNotificationId >= WoopsaSubscriptionServiceConst.MaximumNotificationId)
                    _lastNotificationId = WoopsaSubscriptionServiceConst.MinimumNotificationId;
                notification.Id = _lastNotificationId;
                _pendingNotifications.Enqueue(subscription, notification, out bool itemDiscarded);
                if (itemDiscarded)
                    _notificationsLost = true;
            }
            _waitNotificationEvent.Set();
        }

        internal protected virtual void OnBeforeWoopsaModelAccess()
        {
            BeforeWoopsaModelAccess?.Invoke(this, new EventArgs());
        }

        internal protected virtual void OnAfterWoopsaModelAccess()
        {
            AfterWoopsaModelAccess?.Invoke(this, new EventArgs());
        }

        #endregion

        #region Private Methods

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

        #endregion

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
    }
}
