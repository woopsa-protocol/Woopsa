using System;
using System.Collections.Generic;

using PairSubscriptionNotification = System.Collections.Generic.KeyValuePair<Woopsa.BaseWoopsaSubscriptionServiceSubscription, Woopsa.IWoopsaNotification>;

namespace Woopsa
{
    internal class NotificationConcurrentQueue
    {
        #region Constructor

        public NotificationConcurrentQueue(int maxQueueSize)
        {
            _notifications = new LinkedList<PairSubscriptionNotification>();
            MaxQueueSize = maxQueueSize;
        }

        #endregion

        #region Fields / Attributes

        private LinkedList<PairSubscriptionNotification> _notifications;

        #endregion

        #region Properties

        public int MaxQueueSize { get; }

        public int Count
        {
            get
            {
                lock (_notifications)
                    return _notifications.Count;
            }
        }

        #endregion

        #region Public Methods

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

        public IWoopsaNotification[] PeekNotifications(int maxCount)
        {
            lock (_notifications)
            {
                int count = Math.Min(_notifications.Count, maxCount);
                IWoopsaNotification[] result = new IWoopsaNotification[count];
                LinkedListNode<PairSubscriptionNotification> node = _notifications.First;
                int i = 0;
                while (node != null)
                {
                    result[i] = node.Value.Value;
                    i++;
                    node = node.Next;
                    if (i >= count)
                        break;
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

        public void RemoveNotificationsForSubscription(int subscriptionId)
        {
            lock (_notifications)
            {
                LinkedListNode<PairSubscriptionNotification> node = _notifications.First;
                while (node != null)
                {
                    var next = node.Next;
                    if (node.Value.Value.SubscriptionId == subscriptionId)
                        _notifications.Remove(node);
                    else
                        break;
                    node = next;
                }
            }
        }

        #endregion

        #region Private Methods

        private int notificationAge(int notificationIdOrigin, int notificationId)
        {
            int age = notificationId - notificationIdOrigin;
            if (age < 0)
                age += WoopsaSubscriptionServiceConst.MaximumNotificationId + age; // Note : Id restart from 1
            return age;
        }

        #endregion
    }
}
