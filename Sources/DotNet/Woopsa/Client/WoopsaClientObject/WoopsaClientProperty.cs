using System;

namespace Woopsa
{
    public class WoopsaClientProperty : WoopsaProperty
    {
        #region Constructors

        public WoopsaClientProperty(WoopsaBaseClientObject container, string name, WoopsaValueType type, WoopsaPropertyGet get, WoopsaPropertySet set)
            : base(container, name, type, get, set)
        {
            if (container == null)
                throw new ArgumentNullException("container", string.Format("The argument '{0}' of the WoopsaClientProperty constructor cannot be null!", "container"));
        }

        public WoopsaClientProperty(WoopsaBaseClientObject container, string name, WoopsaValueType type, WoopsaPropertyGet get)
            : this(container, name, type, get, null) { }

        #endregion

        #region Public Methods

        public WoopsaClientSubscription Subscribe(EventHandler<WoopsaNotificationEventArgs> handler, TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            return ((WoopsaBaseClientObject)Owner).Subscribe(Name,
                (sender, e) =>
                {
                    if (handler != null)
                        handler(this, e);
                },
                monitorInterval, publishInterval);
        }

        public WoopsaClientSubscription Subscribe(EventHandler<WoopsaNotificationEventArgs> handler)
        {
            return Subscribe(handler,
                WoopsaSubscriptionServiceConst.DefaultMonitorInterval,
                WoopsaSubscriptionServiceConst.DefaultPublishInterval);
        }

        #endregion
    }
}