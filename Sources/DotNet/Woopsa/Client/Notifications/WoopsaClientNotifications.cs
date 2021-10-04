using System;
using System.Collections.Generic;
using System.Linq;
namespace Woopsa
{
    public class WoopsaClientNotifications : IWoopsaNotifications
    {
        #region Constructor

        public WoopsaClientNotifications()
        {
            _notifications = new List<WoopsaClientNotification>();
        }

        #endregion

        #region Fields / Atriibutes

        private readonly List<WoopsaClientNotification> _notifications;

        #endregion

        #region Public properties

        public IEnumerable<IWoopsaNotification> Notifications => _notifications;

        #endregion

        #region Public Methods

        public void Add(WoopsaClientNotification notification) => _notifications.Add(notification);

        #endregion
    }
}
