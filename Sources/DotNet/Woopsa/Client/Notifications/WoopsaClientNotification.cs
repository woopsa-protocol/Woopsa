using System;
using System.Collections.Generic;
using System.Linq;
namespace Woopsa
{
    public class WoopsaClientNotification : IWoopsaNotification
    {
        #region Constructor

        public WoopsaClientNotification(WoopsaValue value, int subscriptionId)
        {
            Value = value;
            SubscriptionId = subscriptionId;
        }

        #endregion

        #region Properties

        public WoopsaValue Value { get; }

        IWoopsaValue IWoopsaNotification.Value => Value;

        public int SubscriptionId { get; }

        public int Id { get; set; }

        #endregion
    }
}
