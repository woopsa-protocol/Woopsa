﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
	class WoopsaServiceSubscriptionConst
	{
		public static readonly TimeSpan NotificationTimeoutInterval = TimeSpan.FromSeconds(5);
		public static readonly TimeSpan SubscriptionChannelLifeTime = TimeSpan.FromMinutes(20);

        public static readonly TimeSpan DefaultMonitorInterval = TimeSpan.FromMilliseconds(200);
        public static readonly TimeSpan DefaultPublishInterval = TimeSpan.FromMilliseconds(200);

        public const int MaximumNotificationId = 1000000000;

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
		IWoopsaValue CreateSubscriptionChannel(IWoopsaValue notificationQueueSize);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="subscriptionChannel"></param>
		/// <param name="woopsaPropertyLink"></param>
		/// <param name="monitorInterval"></param>
		/// <param name="publishInterval"></param>
		/// <returns>Integer, the identifier of the subscription</returns>
		IWoopsaValue RegisterSubscription(IWoopsaValue subscriptionChannel, IWoopsaValue woopsaPropertyLink, IWoopsaValue monitorInterval, IWoopsaValue publishInterval);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="subscriptionChannel"></param>
		/// <param name="subscriptionId"></param>
		/// <returns>Logical, true if the subsciption has been found and successfully unregistered</returns>
		IWoopsaValue UnregisterSubscription(IWoopsaValue subscriptionChannel, IWoopsaValue subscriptionId);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="subscriptionChannel"></param>
        /// <param name="lastNotificationId"></param>
		/// <returns>JsonData. Json serialization of the IWoopsaNotifications. throws an exception if the channel is not valid</returns>
		IWoopsaValue WaitNotification(IWoopsaValue subscriptionChannel, IWoopsaValue lastNotificationId);
	}

	public interface IWoopsaNotification
	{
        IWoopsaValue Value { get; }
        int SubscriptionId { get; }
        int Id { get; set; }
	}

	public interface IWoopsaNotifications
	{
		IEnumerable<IWoopsaNotification> Notifications { get; }
	}
}
