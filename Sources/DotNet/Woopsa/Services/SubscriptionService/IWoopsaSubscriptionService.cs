using System;
using System.Collections.Generic;

namespace Woopsa
{
    public static class WoopsaSubscriptionServiceConst
    {
        public static readonly TimeSpan WaitNotificationTimeout = TimeSpan.FromSeconds(5);

        public static readonly TimeSpan SubscriptionChannelLifeTimeCheckInterval = TimeSpan.FromSeconds(60);
        public static readonly TimeSpan SubscriptionChannelLifeTime = TimeSpan.FromMinutes(20);

        public static readonly TimeSpan MonitorIntervalLastPublishedValueOnly = TimeSpan.FromMilliseconds(0);
        public static readonly TimeSpan DefaultMonitorInterval = MonitorIntervalLastPublishedValueOnly;

        // In combination with MonitorIntervalLastPublishedValueOnly
        public static readonly TimeSpan PublishIntervalOnce = TimeSpan.FromMilliseconds(0);

        public static readonly TimeSpan DefaultPublishInterval = TimeSpan.FromMilliseconds(100);

        public const int MinimumNotificationId = 1;
        public const int MaximumNotificationId = 1000000000;

        public const int MaxGroupedNotificationCount = 1000;

        public const string WoopsaServiceSubscriptionName = "SubscriptionService";
        public const string WoopsaCreateSubscriptionChannel = "CreateSubscriptionChannel";
        public const string WoopsaRegisterSubscription = "RegisterSubscription";
        public const string WoopsaUnregisterSubscription = "UnregisterSubscription";
        public const string WoopsaWaitNotification = "WaitNotification";

        public const string WoopsaNotificationQueueSize = "NotificationQueueSize";
        public const string WoopsaSubscriptionChannel = "SubscriptionChannel";
        public const string WoopsaPropertyLink = "PropertyLink";
        public const string WoopsaMonitorInterval = "MonitorInterval";
        public const string WoopsaPublishInterval = "PublishInterval";
        public const string WoopsaSubscriptionId = "SubscriptionId";
        public const string WoopsaLastNotificationId = "LastNotificationId";

        public static readonly WoopsaMethodArgumentInfo[] RegisterSubscriptionArguments =
            new WoopsaMethodArgumentInfo[] 
            {
                new WoopsaMethodArgumentInfo("SubscriptionChannel", WoopsaValueType.Integer),
                new WoopsaMethodArgumentInfo("PropertyLink", WoopsaValueType.WoopsaLink),
                new WoopsaMethodArgumentInfo("MonitorInterval", WoopsaValueType.TimeSpan),
                new WoopsaMethodArgumentInfo("PublishInterval", WoopsaValueType.TimeSpan)
            };

        public static readonly WoopsaMethodArgumentInfo[] UnregisterSubscriptionArguments =
            new WoopsaMethodArgumentInfo[]
            {
                new WoopsaMethodArgumentInfo("SubscriptionChannel", WoopsaValueType.Integer),
                new WoopsaMethodArgumentInfo("SubscriptionId", WoopsaValueType.Integer)
            };

        public static readonly TimeSpan ClientSubscriptionUpdateInterval = TimeSpan.FromMilliseconds(1); 

        public static readonly TimeSpan TimeOutUnsubscriptionPerSubscription = TimeSpan.FromMilliseconds(1);
    }

    public interface IWoopsaSubscriptionService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="notificationQueueSize">The maximum number of pending notifications in the channel</param>
        /// <returns>Integer, the identifier of the subscription channel</returns>
        int CreateSubscriptionChannel(int notificationQueueSize);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subscriptionChannel"></param>
        /// <param name="woopsaPropertyLink"></param>
        /// <param name="monitorInterval"></param>
        /// <param name="publishInterval"></param>
        /// <returns>Integer, the identifier of the subscription</returns>
        int RegisterSubscription(int subscriptionChannel, string woopsaPropertyPath, TimeSpan monitorInterval, TimeSpan publishInterval);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subscriptionChannel"></param>
        /// <param name="subscriptionId"></param>
        /// <returns>Logical, true if the subsciption has been found and successfully unregistered</returns>
        bool UnregisterSubscription(int subscriptionChannel, int subscriptionId);

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
        WoopsaJsonData WaitNotification(int subscriptionChannel, int lastNotificationId);
    }

    public interface IWoopsaNotification
    {
        IWoopsaValue Value { get; }
        int SubscriptionId { get; }

        /// <summary>
        /// Id of the notification, range between 1 and 1_000_000_000
        /// </summary>
        int Id { get; set; }
    }

    public interface IWoopsaNotifications
    {
        IEnumerable<IWoopsaNotification> Notifications { get; }
    }
}

