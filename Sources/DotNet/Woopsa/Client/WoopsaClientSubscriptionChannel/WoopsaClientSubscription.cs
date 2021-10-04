using System;

namespace Woopsa
{
    public class WoopsaClientSubscription : IDisposable
    {
        #region Constructor

        internal WoopsaClientSubscription(WoopsaClientSubscriptionChannel channel,
            string servicePath, string relativePath, TimeSpan monitorInterval, TimeSpan publishInterval,
            EventHandler<WoopsaNotificationEventArgs> handler)
        {
            Channel = channel;
            ServicePath = servicePath;
            RelativePath = relativePath;
            MonitorInterval = monitorInterval;
            PublishInterval = publishInterval;
            Handler = handler;
        }

        #endregion

        #region Public Methods

        public void Unsubscribe()
        {
            UnsubscriptionRequested = true;
            Channel.SubscriptionsChanged = true;
        }

        #endregion

        #region Internal Methods

        internal void Execute(WoopsaClientNotification notification)
        {
            if (Handler != null && !UnsubscriptionRequested)
                try
                {
                    Handler(Channel, new WoopsaNotificationEventArgs(notification, this));
                }
                catch (Exception)
                {
                    ;
                    // TODO : how to manage exceptions thrown by notification handlers ?
                }
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                Unsubscribe();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Properties

        public bool IsSubscribed => SubscriptionId != null && !UnsubscriptionRequested;

        public TimeSpan MonitorInterval { get; }
        public TimeSpan PublishInterval { get; }
        public string ServicePath { get; }
        public string RelativePath { get; }
        public EventHandler<WoopsaNotificationEventArgs> Handler { get; }
        public WoopsaClientSubscriptionChannel Channel { get; }

        internal int? SubscriptionId { get; set; }

        internal bool FailedSubscription { get; set; }
        internal bool UnsubscriptionRequested { get; private set; }

        #endregion
    }
}
