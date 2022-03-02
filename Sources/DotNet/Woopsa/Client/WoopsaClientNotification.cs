using System.Collections.Generic;

namespace Woopsa
{
    public class WoopsaClientNotification : IWoopsaNotification
    {
        public WoopsaClientNotification(WoopsaValue value, int subscriptionId)
        {
            Value = value;
            SubscriptionId = subscriptionId;
        }

        public WoopsaValue Value { get; }

        IWoopsaValue IWoopsaNotification.Value => Value;

        public int SubscriptionId { get; }

        public int Id { get; set; }

    }

    public class WoopsaClientNotifications : IWoopsaNotifications
    {
        public WoopsaClientNotifications()
        {
            _notifications = new List<WoopsaClientNotification>();
        }

        public void Add(WoopsaClientNotification notification) => _notifications.Add(notification);

        public IEnumerable<IWoopsaNotification> Notifications => _notifications;

        private readonly List<WoopsaClientNotification> _notifications;
    }
}
