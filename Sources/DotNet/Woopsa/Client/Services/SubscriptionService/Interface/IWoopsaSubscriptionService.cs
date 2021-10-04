using System;

namespace Woopsa
{
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
}

