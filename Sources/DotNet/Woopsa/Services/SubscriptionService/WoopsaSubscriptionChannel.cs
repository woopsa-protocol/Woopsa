using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Woopsa
{
    public class WoopsaSubscriptionChannel : IDisposable
    {
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
        
        public int RegisterSubscription(IWoopsaContainer container, IWoopsaValue woopsaPropertyLink, int monitorInterval, int publishInterval)
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
                    _notifications.Enqueue(notification);
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

        public IWoopsaNotifications WaitNotification(TimeSpan timeout)
        {
            if (WaitHandle.WaitAny(new WaitHandle[]{_waitStopEvent, _waitNotificationEvent}, timeout) != 0)
            {
                lock (_lock)
                {
                    WoopsaNotifications returnNotifications = new WoopsaNotifications();
                    while (!_notifications.IsEmpty)
                    {
                        IWoopsaNotification notification;
                        _notifications.TryDequeue(out notification);
                        returnNotifications.Add(notification);
                    }
                    return returnNotifications;
                }
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
        private int _lastSubscriptionId = 1;
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
