using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Woopsa
{
    public class WoopsaSubscriptionChannel : IDisposable
    {
        // TODO: Delete subscription channels that aren't active after XX minutes
        public WoopsaSubscriptionChannel(int notificationQueueSize)
        {
            lock (_idLock)
            {
                Id = _lastId++;
            }
            NotificationQueueSize = notificationQueueSize;
            _notifications = new ConcurrentQueue<IWoopsaNotification>();
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
        public int NotificationQueueSize { get; set; }

        public int RegisterSubscription(IWoopsaContainer container, IWoopsaValue woopsaPropertyLink, TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            WoopsaSubscription newSubscription;
            lock (_idLock)
            {
                newSubscription = new WoopsaSubscription(container, _lastSubscriptionId, woopsaPropertyLink, monitorInterval, publishInterval);
                _lastSubscriptionId++;
            }
            newSubscription.Publish += newSubscription_Publish;
            _subscriptions.Add(newSubscription.Id, newSubscription);
            
            return newSubscription.Id;
        }

        void newSubscription_Publish(object sender, PublishEventArgs e)
        {
            lock (_lock)
            {
                foreach (var notification in e.Notifications)
                {
                    if (_notifications.Count >= NotificationQueueSize)
                    {
                        // If the queue is full, raise the notificationsLost flag
                        // and remove the oldest notification in the queue.
                        // The WaitNotification method will then throw an exception
                        // until the client has acknowledged the loss of notifications
                        _notificationsLost = true;
                        IWoopsaNotification discardedNotification;
                        _notifications.TryDequeue(out discardedNotification);
                    }
                    _notifications.Enqueue(notification);
                    notification.Id = _lastNotificationId++;
                    if (_lastNotificationId >= WoopsaServiceSubscriptionConst.MaximumNotificationId)
                        _lastNotificationId = WoopsaServiceSubscriptionConst.MaximumNotificationId;
                }
            }
            _waitNotificationEvent.Set();
        }

        public bool UnregisterSubscription(int subscriptionId)
        {
            if (_subscriptions.ContainsKey(subscriptionId))
            {
                var subscription = _subscriptions[subscriptionId];
                /*
                if (  _monitors[subscription.MonitorInterval].RemoveSubscription(subscription) )
                {
                    _monitors[subscription.MonitorInterval].Stop();
                    _monitors.Remove(subscription.MonitorInterval);
                }
                 * */
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

        public IWoopsaNotifications WaitNotification(TimeSpan timeout, int lastNotificationId)
        {
            if (_notificationsLost && lastNotificationId != 0)
                throw new WoopsaNotificationsLostException("Notifications have been lost because the queue was full. Acknowledge the error by calling WaitNotification with LastNotificationId = 0");
            else
                _notificationsLost = false;

            int countNotifications = _notifications.Count;
            bool lastNotificationFound = false;
            for (int i = 0; i < countNotifications; i++)
                if (_notifications.ElementAt(i).Id == lastNotificationId)
                    lastNotificationFound = true;

            if (lastNotificationFound)
            {
                countNotifications = _notifications.Count;
                lock (_lock)
                {
                    for (; ; )
                    {
                        IWoopsaNotification notification;
                        if (_notifications.TryPeek(out notification))
                        {
                            _notifications.TryDequeue(out notification);
                            if (notification.Id == lastNotificationId)
                                break;
                        }
                    }
                }
            }

            if (WaitHandle.WaitAny(new WaitHandle[]{_waitStopEvent, _waitNotificationEvent}, timeout) != 0)
            {
                WoopsaNotifications returnNotifications = new WoopsaNotifications();

                lock (_lock)
                {
                    foreach (IWoopsaNotification notification in _notifications.Where(n => !lastNotificationFound || (n.Id > lastNotificationId)))
                    {
                        returnNotifications.Add(notification);
                    }
                }

                return returnNotifications;
            }
            else
                return new WoopsaNotifications();
        }

        private void monitor_Notification(object sender, NotificationEventArgs e)
        {
            _notifications.Enqueue(e.Notification);
            _waitNotificationEvent.Set();
        }


		private static int _lastId = (int)new Random().NextDouble();
        private bool _notificationsLost = false;
        private int _lastSubscriptionId = 1;
        private int _lastNotificationId = 1;
        private Dictionary<int, WoopsaSubscription> _subscriptions = new Dictionary<int, WoopsaSubscription>();
        private ConcurrentQueue<IWoopsaNotification> _notifications;

        private AutoResetEvent _waitNotificationEvent = new AutoResetEvent(false);
        private AutoResetEvent _waitStopEvent = new AutoResetEvent(false);
        private AutoResetEvent _waitPublishEvent = new AutoResetEvent(false);

        private object _lock = new object();
        private object _idLock = new Object();

        
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _waitNotificationEvent.Dispose();
                _waitStopEvent.Dispose();
                _waitPublishEvent.Dispose();
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
