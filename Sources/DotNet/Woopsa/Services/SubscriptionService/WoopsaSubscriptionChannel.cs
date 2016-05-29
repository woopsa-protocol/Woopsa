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
            lock(_idLock)
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
            WoopsaSubscription newSubscription;
            lock(_idLock)
            {
                _lastSubscriptionId++;
                newSubscription = new WoopsaSubscription(container, _lastSubscriptionId, woopsaPropertyLink, monitorInterval, publishInterval);
            }
            newSubscription.Publish += newSubscription_Publish;
            _subscriptions.Add(newSubscription.Id, newSubscription);
            _watchClientActivity.Restart();
            return newSubscription.Id;
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
            if (_subscriptions.ContainsKey(subscriptionId))
            {
                var subscription = _subscriptions[subscriptionId];
                subscription.Dispose();
                _subscriptions.Remove(subscriptionId);
                _watchClientActivity.Restart();
                return true;
            }
            else
                return false;
        }

        public void Stop()
        {
            _waitStopEvent.Set();
        }

        public IWoopsaNotifications WaitNotification(TimeSpan timeout, int lastNotificationId)
        {
            bool hasNotifications;
            _watchClientActivity.Restart();
            // Remove acknowledged notifications already sent to the client
            if (_notificationsLost && lastNotificationId != 0)
                throw new WoopsaNotificationsLostException("Notifications have been lost because the queue was full. Acknowledge the error by calling WaitNotification with LastNotificationId = 0");
            else
                _notificationsLost = false;
            IWoopsaNotification notification;
            while (_pendingNotifications.Count > 0)
            {
                if (_pendingNotifications.TryPeek(out notification))
                {
                    if (notificationAge(notification.Id) <= notificationAge(lastNotificationId))
                    {
                        _pendingNotifications.TryDequeue(out notification);
                        _lastRemovedNotificationId = notification.Id;
                    }
                    else
                        break;
                }
            }
            // Wait notifications
            _waitNotificationEvent.Reset();
            hasNotifications = _pendingNotifications.Count > 0;
            if (!hasNotifications)
                WaitHandle.WaitAny(new WaitHandle[] { _waitStopEvent, _waitNotificationEvent }, timeout);
            // Prepare result
            WoopsaNotifications result = new WoopsaNotifications();
            while (_pendingNotifications.TryDequeue(out notification))
                result.Add(notification);
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

        private static int _lastChannelId = 0;
        private bool _notificationsLost = false;
        private int _lastSubscriptionId = 0;
        private int _lastNotificationId = 0;
        private int _lastRemovedNotificationId = 0;
        private Dictionary<int, WoopsaSubscription> _subscriptions = new Dictionary<int, WoopsaSubscription>();
        private ConcurrentQueue<IWoopsaNotification> _pendingNotifications;

        private AutoResetEvent _waitNotificationEvent = new AutoResetEvent(false);
        private AutoResetEvent _waitStopEvent = new AutoResetEvent(false);

        private object _idLock = new Object();

        private Stopwatch _watchClientActivity;


        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();
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
