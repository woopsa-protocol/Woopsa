using System;
using System.Collections.Generic;

namespace Woopsa
{
    public static class WoopsaServiceSubscriptionConst
	{
		public static readonly TimeSpan NotificationTimeoutInterval = TimeSpan.FromSeconds(5);
		public static readonly TimeSpan SubscriptionChannelLifeTime = TimeSpan.FromMinutes(20);

        public static readonly TimeSpan DefaultMonitorInterval = TimeSpan.FromMilliseconds(200);
        public static readonly TimeSpan DefaultPublishInterval = TimeSpan.FromMilliseconds(200);

        public const int MinimumNotificationId = 1;
        public const int MaximumNotificationId = 1000000000;
        public static readonly TimeSpan ClientTimeOut = TimeSpan.FromMinutes(20);

        public const string WoopsaServiceSubscriptionName   = "SubscriptionService";
        public const string WoopsaCreateSubscriptionChannel = "CreateSubscriptionChannel";
        public const string WoopsaRegisterSubscription      = "RegisterSubscription";
        public const string WoopsaUnregisterSubscription    = "UnregisterSubscription";
        public const string WoopsaWaitNotification          = "WaitNotification";

        public const string WoopsaNotificationQueueSize     = "NotificationQueueSize";
        public const string WoopsaSubscriptionChannel       = "SubscriptionChannel";
        public const string WoopsaPropertyLink              = "PropertyLink";
        public const string WoopsaMonitorInterval           = "MonitorInterval";
        public const string WoopsaPublishInterval           = "PublishInterval";
        public const string WoopsaSubscriptionId            = "SubscriptionId";
        public const string WoopsaLastNotificationId        = "LastNotificationId";
    }
    public interface IWoopsaServiceSubscription
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="notificationQueueSize">The maximum number of pending notifications in the channel</param>
		/// <returns>Integer, the identifier of the subscription channel</returns>
		WoopsaValue CreateSubscriptionChannel(IWoopsaValue notificationQueueSize);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="subscriptionChannel"></param>
		/// <param name="woopsaPropertyLink"></param>
		/// <param name="monitorInterval"></param>
		/// <param name="publishInterval"></param>
		/// <returns>Integer, the identifier of the subscription</returns>
		WoopsaValue RegisterSubscription(IWoopsaValue subscriptionChannel, IWoopsaValue woopsaPropertyLink, IWoopsaValue monitorInterval, IWoopsaValue publishInterval);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="subscriptionChannel"></param>
		/// <param name="subscriptionId"></param>
		/// <returns>Logical, true if the subsciption has been found and successfully unregistered</returns>
		WoopsaValue UnregisterSubscription(IWoopsaValue subscriptionChannel, IWoopsaValue subscriptionId);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="subscriptionChannel"></param>
        /// <param name="lastNotificationId">
        /// The id of the last notification received by the client. All the notifications upt o and including this one
        /// will be deleted from channel queue.
        /// Set to 0 to acknowledge a notification queue overflow.
        /// </param>
		/// <returns>JsonData. Json serialization of the IWoopsaNotifications. throws an exception if the channel is not valid</returns>
		WoopsaValue WaitNotification(IWoopsaValue subscriptionChannel, IWoopsaValue lastNotificationId);
	}

	public interface IWoopsaNotification
	{
        IWoopsaValue Value { get; }
        int SubscriptionId { get; }

        /// <summary>
        /// Id range between 1 and 1_000_000_000
        /// </summary>
        int Id { get; set; }
	}

	public interface IWoopsaNotifications
	{
		IEnumerable<IWoopsaNotification> Notifications { get; }
	}
}
