using System;

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
        public const string WoopsaCreateSubscriptionChannelAsync = "CreateSubscriptionChannelAsync";
        public const string WoopsaRegisterSubscription = "RegisterSubscription";
        public const string WoopsaUnregisterSubscription = "UnregisterSubscription";
        public const string WoopsaWaitNotification = "WaitNotification";
        public const string WoopsaWaitNotificationAsync = "WaitNotificationAsync";

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
}

