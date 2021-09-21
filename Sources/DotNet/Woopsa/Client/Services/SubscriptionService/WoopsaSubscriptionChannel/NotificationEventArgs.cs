using System;

namespace Woopsa
{
    public class NotificationEventArgs : EventArgs
    {
        #region Constructor

        public NotificationEventArgs(WoopsaServerNotification notification)
        {
            Notification = notification;
        }

        #endregion

        #region Properties

        public WoopsaServerNotification Notification { get; }


        #endregion
    }
}
