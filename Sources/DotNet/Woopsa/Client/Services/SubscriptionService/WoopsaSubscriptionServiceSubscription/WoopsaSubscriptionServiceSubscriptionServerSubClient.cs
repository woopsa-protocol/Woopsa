using System;

namespace Woopsa
{
    public class WoopsaSubscriptionServiceSubscriptionServerSubClient :
        BaseWoopsaSubscriptionServiceSubscription
    {
        #region Constructors

        public WoopsaSubscriptionServiceSubscriptionServerSubClient(
        WoopsaSubscriptionChannel channel,
        WoopsaContainer root,
        int subscriptionId, string propertyPath,
        TimeSpan monitorInterval, TimeSpan publishInterval,
        WoopsaBaseClientObject subClient, string relativePropertyPath) :
            base(channel, root, subscriptionId, propertyPath, monitorInterval, publishInterval)
        {
            bool isSingleNotification =
                monitorInterval == WoopsaSubscriptionServiceConst.MonitorIntervalLastPublishedValueOnly &&
                publishInterval == WoopsaSubscriptionServiceConst.PublishIntervalOnce;
            EventHandler<WoopsaNotificationEventArgs> handler;
            if (isSingleNotification)
                handler =
                    (sender, e) =>
                    {
                        EnqueueNewMonitoredValue(e.Notification.Value);
                        DoPublish(); // there is not publish timer, force publishing the unique notification
                    };
            else
                handler =
                    (sender, e) =>
                    {
                        EnqueueNewMonitoredValue(e.Notification.Value);
                    };
            _clientSubscription = subClient.Subscribe(relativePropertyPath, handler,
                    monitorInterval, publishInterval);
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (_clientSubscription != null)
                {
                    _clientSubscription.Dispose();
                    _clientSubscription = null;
                }
            }
        }

        #endregion

        #region Fields / Attributes

        private WoopsaClientSubscription _clientSubscription;

        #endregion
    }
}