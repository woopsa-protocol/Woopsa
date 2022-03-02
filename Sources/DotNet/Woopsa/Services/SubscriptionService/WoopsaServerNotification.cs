using System.Collections.Generic;

namespace Woopsa
{
    public class WoopsaServerNotification : IWoopsaNotification
    {
        public WoopsaServerNotification(IWoopsaValue value, int subscriptionId)
        {
            Value = value;
            SubscriptionId = subscriptionId;
        }

        public IWoopsaValue Value { get; }

        public int SubscriptionId { get; }

        public int Id { get; set; }
    }

    public class WoopsaServerNotifications : IWoopsaNotifications
    {
        public WoopsaServerNotifications()
        {
            _notifications = new List<IWoopsaNotification>();
        }

        public void Add(IWoopsaNotification notification)
        {
            _notifications.Add(notification);
        }

        public void AddRange(IEnumerable<IWoopsaNotification> notification)
        {
            _notifications.AddRange(notification);
        }

        public IEnumerable<IWoopsaNotification> Notifications => _notifications;

        private readonly List<IWoopsaNotification> _notifications;
    }
}
