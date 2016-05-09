using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public IEnumerable<IWoopsaNotification> Notifications
        {
            get 
            {
                return _notifications;
            }
        }

        public void Add(IWoopsaNotification notification)
        {
            _notifications.Add(notification);
        }

        private List<IWoopsaNotification> _notifications = new List<IWoopsaNotification>();
    }
}
