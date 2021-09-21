using System;

namespace Woopsa
{
    public class WoopsaNotificationEventArgs : EventArgs
    {
        #region Constructor

        public WoopsaNotificationEventArgs(WoopsaClientNotification notification,
            WoopsaClientSubscription subscription)
        {
            Notification = notification;
            Subscription = subscription;
        }

        #endregion

        #region Properties

        public WoopsaClientNotification Notification { get; }
        public WoopsaClientSubscription Subscription { get; }

        #endregion
    }
}
