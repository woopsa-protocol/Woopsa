namespace Woopsa
{
    public interface IWoopsaNotification
    {
        IWoopsaValue Value { get; }
        int SubscriptionId { get; }

        /// <summary>
        /// Id of the notification, range between 1 and 1_000_000_000
        /// </summary>
        int Id { get; set; }
    }
}

