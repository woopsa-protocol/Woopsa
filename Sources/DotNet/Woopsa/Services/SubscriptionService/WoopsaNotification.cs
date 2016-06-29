using System.Collections.Generic;

namespace Woopsa
{
    public class WoopsaNotification : IWoopsaNotification
    {
        public WoopsaNotification(IWoopsaValue value, int subscriptionId)
        {
            Value = value;
            SubscriptionId = subscriptionId;
        }

        public IWoopsaValue Value { get; private set; }

        public int SubscriptionId { get; private set; }

        public int Id { get; set; }
    }

    public class WoopsaNotifications : IWoopsaNotifications
    {
        public WoopsaNotifications()
        {
            _notifications = new List<IWoopsaNotification>();
        }

        public void Add(IWoopsaNotification notification)
        {
            _notifications.Add(notification);
        }

        public IEnumerable<IWoopsaNotification> Notifications
        {
            get { return _notifications; }
        }

        private readonly List<IWoopsaNotification> _notifications;
    }
}
