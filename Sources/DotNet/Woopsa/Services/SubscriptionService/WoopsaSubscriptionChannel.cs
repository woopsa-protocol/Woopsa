using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Woopsa
{
    public class WoopsaSubscriptionChannel : IDisposable
    {
        public WoopsaSubscriptionChannel(int notificationQueueSize)
        {
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
            get { return _watchClientActivity.Elapsed > WoopsaServiceSubscriptionConst.ClientTimeOut; }
        }

        public int RegisterSubscription(WoopsaContainer container, IWoopsaValue woopsaPropertyLink, TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            WoopsaServerSubscription newSubscription;
            int subscriptionId;
            _watchClientActivity.Restart();
            lock (_idLock)
            {
                _lastSubscriptionId++;
                subscriptionId = _lastSubscriptionId;
            }
            newSubscription = new WoopsaServerSubscription(container, subscriptionId, woopsaPropertyLink, monitorInterval, publishInterval);
            newSubscription.Publish += newSubscription_Publish;
            lock (_subscriptions)
                _subscriptions.Add(newSubscription.SubscriptionId, newSubscription);
            return newSubscription.SubscriptionId;
        }

        private void newSubscription_Publish(object sender, PublishEventArgs e)
        {
            foreach (var notification in e.Notifications)
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
                if (_lastNotificationId >= WoopsaServiceSubscriptionConst.MaximumNotificationId)
                    _lastNotificationId = WoopsaServiceSubscriptionConst.MinimumNotificationId;
                notification.Id = _lastNotificationId;
                _pendingNotifications.Enqueue(notification);
            }
            _waitNotificationEvent.Set();
        }

        public bool UnregisterSubscription(int subscriptionId)
        {
            WoopsaServerSubscription subscription;
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
            WoopsaNotifications result = new WoopsaNotifications();

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
                age += WoopsaServiceSubscriptionConst.MaximumNotificationId + age;
            return age;
        }

        private static Random _random = new Random();
        private static int _lastChannelId = _random.Next();
        private bool _notificationsLost = false;
        private int _lastSubscriptionId = 0;
        private int _lastNotificationId = 0;
        private int _lastRemovedNotificationId = 0;
        private Dictionary<int, WoopsaServerSubscription> _subscriptions = new Dictionary<int, WoopsaServerSubscription>();
        private ConcurrentQueue<IWoopsaNotification> _pendingNotifications;

        private AutoResetEvent _waitNotificationEvent = new AutoResetEvent(false);
        private ManualResetEvent _waitStopEvent = new ManualResetEvent(false);

        private object _idLock = new Object();

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
        public NotificationEventArgs(WoopsaNotification notification)
        {
            Notification = notification;
        }

        public WoopsaNotification Notification { get; private set; }
    }
}
