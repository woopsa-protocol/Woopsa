using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace Woopsa
{
    public class WoopsaClientSubscriptionChannel
    {
        public static readonly TimeSpan ReconnectionInterval = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// returns the current subscription channel in which we are executing.
        /// return null if the current context is not a thread of the subscription channel.
        /// </summary>
        public static WoopsaClientSubscriptionChannel CurrentChannel { get { return _currentChannel; } }

        public WoopsaClientSubscriptionChannel(WoopsaUnboundClientObject woopsaRoot,
            int notificationQueueSize)
        {
            _woopsaRoot = woopsaRoot;
            _woopsaSubscribeService = _woopsaRoot.GetUnboundItem(
                WoopsaSubscriptionServiceConst.WoopsaServiceSubscriptionName);
            CreateWoopsaSubscriptionServiceMethods();
            _notificationQueueSize = notificationQueueSize;
            _subscriptions = new List<WoopsaClientSubscription>();
            _registeredSubscriptions = new Dictionary<int, WoopsaClientSubscription>();
        }

        public WoopsaClientSubscription Subscribe(string path,
            EventHandler<WoopsaNotificationEventArgs> handler,
            TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            return Subscribe(path, path, handler, monitorInterval, publishInterval);
        }

        public WoopsaClientSubscription Subscribe(string path,
            EventHandler<WoopsaNotificationEventArgs> handler)
        {
            return Subscribe(path, path, handler,
                WoopsaSubscriptionServiceConst.DefaultMonitorInterval,
                WoopsaSubscriptionServiceConst.DefaultPublishInterval);
        }

        public bool Terminated { get { return _terminated; } }

        public List<WoopsaClientSubscription> GetFailedSubscriptions()
        {
            List<WoopsaClientSubscription> failedSubscriptions = null;
            lock (_subscriptions)
                foreach (var item in _subscriptions)
                    if (item.FailedSubscription)
                    {
                        if (failedSubscriptions == null)
                            failedSubscriptions = new List<WoopsaClientSubscription>();
                        failedSubscriptions.Add(item);
                    }
            return failedSubscriptions;
        }

        public void Terminate()
        {
            _terminated = true;
            if (_localSubscriptionService != null)
                _localSubscriptionService.Terminate();
        }

        internal WoopsaClientSubscription Subscribe(string servicePath, string relativePath,
             EventHandler<WoopsaNotificationEventArgs> handler,
             TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            WoopsaClientSubscription subscription =
                new WoopsaClientSubscription(this, servicePath, relativePath, monitorInterval, publishInterval, handler);
            lock (_subscriptions)
                _subscriptions.Add(subscription);
            EnsureServiceThreadStarted();
            return subscription;
        }

        private void EnsureServiceThreadStarted()
        {
            if (_thread == null)
            {
                _thread = new Thread(new ThreadStart(executeService));
                _thread.Name = "WoopsaClientSubscription";
                _thread.Start();
            }
        }

        private void executeService()
        {
            _currentChannel = this;
            while (!_terminated)
                try
                {
                    if (_subscriptionOpenChannel == null)
                        OpenChannel();
                    else
                    {
                        bool hasSubscriptions;
                        lock (_subscriptions)
                            hasSubscriptions = _subscriptions.Count > 0;
                        if (hasSubscriptions)
                        {
                            ManageSubscriptions();
                            ProcessNotifications();
                        }
                        else
                            Thread.Sleep(TimeSpan.FromMilliseconds(1));
                    }
                }
                catch (WoopsaInvalidSubscriptionChannelException)
                {
                    CloseChannel();
                }
                catch (WebException)
                {
                    if (!_terminated)
                    {
                        // There was some sort of network error. We will
                        // try again later
                        Thread.Sleep(ReconnectionInterval);
                    }
                    else
                    { // Cancelled WebRequest, ignore
                    }
                }
                catch (ObjectDisposedException)
                {
                }
                catch (Exception)
                {
                }
        }

        private void OpenChannel()
        {
            try
            {
                _subscriptionOpenChannel = CreateSubscriptionChannel(_notificationQueueSize);
            }
            catch (WoopsaNotFoundException)
            {
                // No subscription service available, create a local one
                _localSubscriptionService = new WoopsaSubscriptionServiceImplementation(_woopsaRoot, false);
                try
                {
                    _subscriptionOpenChannel = CreateSubscriptionChannel(_notificationQueueSize);
                    //TODO : détecter la perte de connection du service de souscription et fermer le canal
                }
                catch
                {
                    _localSubscriptionService.Dispose();
                    throw;
                }
            }
        }

        private void CloseChannel()
        {
            if (_localSubscriptionService != null)
            {
                _localSubscriptionService.Dispose();
                _localSubscriptionService = null;
            }
            _subscriptionOpenChannel = null;
            _lastNotificationId = 0;
            // mark all subscriptions as unsubscribed
            lock (_subscriptions)
            {
                foreach (var item in _subscriptions)
                    item.SubscriptionId = null;
                _registeredSubscriptions.Clear();
            }
        }

        private void ManageSubscriptions()
        {
            // New subscriptions
            List<WoopsaClientSubscription> newSubscriptions = null;
            lock (_subscriptions)
                foreach (var item in _subscriptions)
                    if (item.SubscriptionId == null)
                    {
                        if (newSubscriptions == null)
                            newSubscriptions = new List<WoopsaClientSubscription>();
                        newSubscriptions.Add(item);
                    }
            if (newSubscriptions != null)
                foreach (var item in newSubscriptions)
                {
                    item.SubscriptionId = RegisterSubscription(
                        _subscriptionOpenChannel.Value, item.ServicePath, item.MonitorInterval, item.PublishInterval);
                    item.FailedSubscription = item.SubscriptionId == null;
                    if (!item.FailedSubscription)
                        _registeredSubscriptions[item.SubscriptionId.Value] = item;
                }
            // New unsubscriptions
            List<WoopsaClientSubscription> newUnsubscriptions = null;
            lock (_subscriptions)
                foreach (var item in _subscriptions)
                    if (item.UnsubscriptionRequested)
                    {
                        if (newUnsubscriptions == null)
                            newUnsubscriptions = new List<WoopsaClientSubscription>();
                        newUnsubscriptions.Add(item);
                    }
            if (newUnsubscriptions != null)
                foreach (var item in newUnsubscriptions)
                {
                    if (item.SubscriptionId.HasValue)
                        UnregisterSubscription(_subscriptionOpenChannel.Value, item.SubscriptionId.Value);
                    item.SubscriptionId = null;
                    lock (_subscriptions)
                    {
                        if (item.SubscriptionId.HasValue)
                            _registeredSubscriptions.Remove(item.SubscriptionId.Value);
                        _subscriptions.Remove(item);
                    }
                }
        }

        private void ProcessNotifications()
        {
            WoopsaClientNotifications notifications;
            try
            {
                if (RetrieveNotification(out notifications, _lastNotificationId) > 0)
                    ExecuteNotifications(notifications);
            }
            catch (WoopsaNotificationsLostException)
            {
                // This happens when we haven't "collected" notifications
                // fast enough and the queue has been filled up completely,
                // forcing past notifications to be erased. We must 
                // acknowledge to the server.
                if (RetrieveNotification(out notifications, 0) > 0)
                    ExecuteNotifications(notifications);
            }
        }

        private int RetrieveNotification(out WoopsaClientNotifications notificationsResult, int lastNotificationId)
        {
            WoopsaJsonData results = WaitNotification(_subscriptionOpenChannel.Value, lastNotificationId);

            var notificationsList = new WoopsaClientNotifications();

            int count = 0;
            for (int i = 0; i < results.Length; i++)
            {
                WoopsaJsonData notification = results[i];
                var notificationValue = notification["Value"];
                var notificationSubscriptionId = notification["SubscriptionId"];
                var notificationId = notification["Id"];
                WoopsaValueType type = (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), notificationValue["Type"]);
                WoopsaValue value = WoopsaValue.CreateChecked(notificationValue["Value"], type, DateTime.Parse(notificationValue["TimeStamp"]));
                WoopsaClientNotification newNotification = new WoopsaClientNotification(value, notificationSubscriptionId);
                newNotification.Id = notificationId;
                notificationsList.Add(newNotification);
                count++;
            }
            notificationsResult = notificationsList;
            return count;
        }

        private void ExecuteNotifications(WoopsaClientNotifications notifications)
        {
            foreach (WoopsaClientNotification notification in notifications.Notifications)
            {
                if (_registeredSubscriptions.ContainsKey(notification.SubscriptionId))
                    _registeredSubscriptions[notification.SubscriptionId].Execute(notification);
                if (notification.Id > _lastNotificationId)
                    _lastNotificationId = notification.Id;
            }
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _terminated = true;
                CloseChannel();
                if (_thread != null)
                {
                    _thread.Join();
                    _thread = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        private void CreateWoopsaSubscriptionServiceMethods()
        {
            _remoteMethodCreateSubscriptionChannel = _woopsaSubscribeService.GetMethod(
                WoopsaSubscriptionServiceConst.WoopsaCreateSubscriptionChannel,
                new WoopsaMethodArgumentInfo[] {
                    new WoopsaMethodArgumentInfo(
                        WoopsaSubscriptionServiceConst.WoopsaNotificationQueueSize,
                        WoopsaValueType.Integer)
                },
                WoopsaValueType.Integer);
            _remoteMethodRegisterSubscription = _woopsaSubscribeService.GetMethod(
                WoopsaSubscriptionServiceConst.WoopsaRegisterSubscription,
                new WoopsaMethodArgumentInfo[] {
                    new WoopsaMethodArgumentInfo(
                        WoopsaSubscriptionServiceConst.WoopsaSubscriptionChannel,
                        WoopsaValueType.Integer),
                    new WoopsaMethodArgumentInfo(
                        WoopsaSubscriptionServiceConst.WoopsaPropertyLink,
                        WoopsaValueType.WoopsaLink),
                    new WoopsaMethodArgumentInfo(
                        WoopsaSubscriptionServiceConst.WoopsaMonitorInterval,
                        WoopsaValueType.TimeSpan),
                    new WoopsaMethodArgumentInfo(
                        WoopsaSubscriptionServiceConst.WoopsaPublishInterval,
                        WoopsaValueType.TimeSpan)
                },
                WoopsaValueType.Integer);
            _remoteMethodUnregisterSubscription = _woopsaSubscribeService.GetMethod(
                WoopsaSubscriptionServiceConst.WoopsaUnregisterSubscription,
                new WoopsaMethodArgumentInfo[] {
                    new WoopsaMethodArgumentInfo(
                        WoopsaSubscriptionServiceConst.WoopsaSubscriptionChannel,
                        WoopsaValueType.Integer),
                    new WoopsaMethodArgumentInfo(
                        WoopsaSubscriptionServiceConst.WoopsaSubscriptionId,
                        WoopsaValueType.Integer)
                },
                WoopsaValueType.Integer);
            _remoteMethodWaitNotification = _woopsaSubscribeService.GetMethod(
                WoopsaSubscriptionServiceConst.WoopsaWaitNotification,
                new WoopsaMethodArgumentInfo[] {
                    new WoopsaMethodArgumentInfo(
                        WoopsaSubscriptionServiceConst.WoopsaSubscriptionChannel,
                        WoopsaValueType.Integer),
                    new WoopsaMethodArgumentInfo(
                        WoopsaSubscriptionServiceConst.WoopsaLastNotificationId,
                        WoopsaValueType.Integer)
                },
                WoopsaValueType.JsonData);
        }

        private int CreateSubscriptionChannel(int notificationQueueSize)
        {
            if (_localSubscriptionService == null)
                return _remoteMethodCreateSubscriptionChannel.Invoke(notificationQueueSize);
            else
                return _localSubscriptionService.CreateSubscriptionChannel(notificationQueueSize);
        }

        private WoopsaJsonData WaitNotification(int subscriptionChannel, int lastNotificationId)
        {
            if (_localSubscriptionService == null)
                return _remoteMethodWaitNotification.Invoke(subscriptionChannel, lastNotificationId).JsonData;
            else
                return _localSubscriptionService.WaitNotification(subscriptionChannel, lastNotificationId);
        }

        private int? RegisterSubscription(int subscriptionOpenChannel, string servicePath, TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            try
            {
                if (_localSubscriptionService == null)
                    return _remoteMethodRegisterSubscription.Invoke(subscriptionOpenChannel, servicePath, monitorInterval, publishInterval);
                else
                    return _localSubscriptionService.RegisterSubscription(subscriptionOpenChannel, servicePath, monitorInterval, publishInterval);
            }
            catch (Exception)
            {
                return null;
            }
        }
        private bool UnregisterSubscription(int subscriptionOpenChannel, int subscriptionId)
        {
            if (_localSubscriptionService == null)
                return _remoteMethodUnregisterSubscription.Invoke(subscriptionOpenChannel, subscriptionId);
            else
                return _localSubscriptionService.UnregisterSubscription(subscriptionOpenChannel, subscriptionId);
        }

        [ThreadStatic]
        private static WoopsaClientSubscriptionChannel _currentChannel;

        private WoopsaUnboundClientObject _woopsaRoot, _woopsaSubscribeService;
        private WoopsaMethod _remoteMethodCreateSubscriptionChannel, _remoteMethodWaitNotification,
            _remoteMethodRegisterSubscription, _remoteMethodUnregisterSubscription;

        private int _notificationQueueSize;
        private List<WoopsaClientSubscription> _subscriptions;
        private Dictionary<int, WoopsaClientSubscription> _registeredSubscriptions;
        private Thread _thread;
        private bool _terminated;

        private int? _subscriptionOpenChannel;
        private WoopsaSubscriptionServiceImplementation _localSubscriptionService;
        private int _lastNotificationId;
    }

    public class WoopsaClientSubscription : IDisposable
    {
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

        public void Unsubscribe()
        {
            UnsubscriptionRequested = true;
        }

        internal void Execute(WoopsaClientNotification notification)
        {
            if (Handler != null && !UnsubscriptionRequested)
                Handler(Channel, new WoopsaNotificationEventArgs(notification, this));
        }

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

        public bool IsSubscribed
        {
            get { return SubscriptionId != null && !UnsubscriptionRequested; }
        }

        public TimeSpan MonitorInterval { get; private set; }
        public TimeSpan PublishInterval { get; private set; }
        public string ServicePath { get; private set; }
        public string RelativePath { get; private set; }
        public EventHandler<WoopsaNotificationEventArgs> Handler { get; private set; }
        public WoopsaClientSubscriptionChannel Channel { get; private set; }

        internal int? SubscriptionId { get; set; }

        internal bool FailedSubscription { get; set; }
        internal bool UnsubscriptionRequested { get; private set; }
    }

    public class WoopsaNotificationEventArgs : EventArgs
    {
        public WoopsaNotificationEventArgs(WoopsaClientNotification notification,
            WoopsaClientSubscription subscription)
        {
            Notification = notification;
            Subscription = subscription;
        }

        public WoopsaClientNotification Notification { get; private set; }
        public WoopsaClientSubscription Subscription { get; private set; }
    }

}
