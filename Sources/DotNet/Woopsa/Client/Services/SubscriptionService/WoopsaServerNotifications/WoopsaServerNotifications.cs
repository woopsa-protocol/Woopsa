using System.Collections.Generic;

namespace Woopsa
{
    public class WoopsaServerNotifications : IWoopsaNotifications
    {
        #region Constructors

        public WoopsaServerNotifications()
        {
            _notifications = new List<IWoopsaNotification>();
        }

        #endregion

        #region Fields / Attributes

        private readonly List<IWoopsaNotification> _notifications;

        #endregion

        #region Properties

        public IEnumerable<IWoopsaNotification> Notifications => _notifications;

        #endregion

        #region Public Methods

        public void Add(IWoopsaNotification notification)
        {
            _notifications.Add(notification);
        }

        public void AddRange(IEnumerable<IWoopsaNotification> notification)
        {
            _notifications.AddRange(notification);
        }

        #endregion
    }
}
