namespace Woopsa
{
    public class WoopsaServerNotification : IWoopsaNotification
    {
        #region Constructor

        public WoopsaServerNotification(IWoopsaValue value, int subscriptionId)
        {
            Value = value;
            SubscriptionId = subscriptionId;
        }

        #endregion

        #region Properties

        public IWoopsaValue Value { get; }

        public int SubscriptionId { get; }

        public int Id { get; set; }

        #endregion
    }
}
