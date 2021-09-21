using System;

namespace Woopsa
{
    public class CanWatchEventArgs: EventArgs
    {
        #region Constructor

        public CanWatchEventArgs(
            BaseWoopsaSubscriptionServiceSubscription subscription, IWoopsaProperty itemProperty)
        {
            Subscription = subscription;
            ItemProperty = itemProperty;
            CanWatch = WoopsaSubscriptionServiceImplementation.
                CanWatchDefaultValue;
        }

        #endregion

        #region Properties

        public BaseWoopsaSubscriptionServiceSubscription Subscription { get; }

        public IWoopsaProperty ItemProperty { get; }

        public bool CanWatch { get; set; }

        #endregion
    }
}
