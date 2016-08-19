using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public WoopsaClientSubscriptionChannel(WoopsaClient client,
            WoopsaUnboundClientObject woopsaRoot, int notificationQueueSize)
        {
            _client = client;
            _woopsaRoot = woopsaRoot;
            _woopsaSubscribeService = _woopsaRoot.GetUnboundItem(
                WoopsaSubscriptionServiceConst.WoopsaServiceSubscriptionName);
            CreateWoopsaSubscriptionServiceMethods();
            _notificationQueueSize = notificationQueueSize;
            _subscriptions = new List<WoopsaClientSubscription>();
            _registeredSubscriptions = new Dictionary<int, WoopsaClientSubscription>();
            _lock = new object();
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

        private void RequestAllUnregistration()
        {
            lock (_subscriptions)
            {
                foreach (var item in _subscriptions)
                    item.Unsubscribe();
            }
            Stopwatch watch = new Stopwatch();
            watch.Start();            
            while (_registeredSubscriptions.Count > 0)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(1));
                if (watch.Elapsed > WoopsaSubscriptionServiceConst.TimeOutUnsubscription)
                    break;
            }
        }

        public void Terminate()
        {
            RequestAllUnregistration();
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
            {
                _subscriptions.Add(subscription);
                SubscriptionsChanged = true;
            }
            EnsureServiceThreadStarted();
            return subscription;
        }

        private void EnsureServiceThreadStarted()
        {
            if (_threadNotifications == null)
            {
                _threadNotifications = new Thread(new ThreadStart(executeServiceNotifications));
                _threadNotifications.Name = "WoopsaClientSubscriptionChannelNotifications";
                _threadNotifications.Start();
            }
            if (_threadSubscriptions == null)
            {
                _threadSubscriptions = new Thread(new ThreadStart(executeServiceSubscriptions));
                _threadSubscriptions.Name = "WoopsaClientSubscriptionChannelSubscriptions";
                _threadSubscriptions.Start();
            }
        }

        private void executeServiceNotifications()
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
                            ProcessNotifications();
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

        private void executeServiceSubscriptions()
        {
            _currentChannel = this;
            while (!_terminated)
                try
                {
                    lock (_lock)
                        if (_subscriptionOpenChannel != null)
                            if (SubscriptionsChanged)
                                ManageSubscriptions();
                    // do not manage to quickly the subscriptions update to improve 
                    // grouping of subscriptions into a single multirequest
                    Thread.Sleep(WoopsaSubscriptionServiceConst.ClientSubscriptionUpdateInterval);
                }
                catch (Exception)
                {
                }
        }

        private void OpenChannel()
        {
            lock (_lock)
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
        }

        private void CloseChannel()
        {
            lock (_lock)
            {
                if (_localSubscriptionService != null)
                {
                    _localSubscriptionService.Dispose();
                    _localSubscriptionService = null;
                }
                _subscriptionOpenChannel = null;
                _lastNotificationId = 0;
                foreach (var item in _subscriptions)
                    item.SubscriptionId = null;
                SubscriptionsChanged = true;
            }
        }


        private void ManageSubscriptions()
        {
            // New subscriptions
            List<WoopsaClientSubscription> newSubscriptions = null;
            SubscriptionsChanged = false;
            lock (_subscriptions)
                foreach (var item in _subscriptions)
                    if (item.SubscriptionId == null)
                    {
                        if (newSubscriptions == null)
                            newSubscriptions = new List<WoopsaClientSubscription>();
                        newSubscriptions.Add(item);
                    }
            if (newSubscriptions != null)
                RegisterSubscriptions(newSubscriptions);
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
                UnregisterSubscriptions(newUnsubscriptions);
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
                if (_threadSubscriptions != null)
                {
                    _threadSubscriptions.Join();
                    _threadSubscriptions = null;
                }
                if (_threadNotifications != null)
                {
                    _threadNotifications.Join();
                    _threadNotifications = null;
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
                WoopsaValueType.Integer,
                new WoopsaMethodArgumentInfo[] {
                    new WoopsaMethodArgumentInfo(
                        WoopsaSubscriptionServiceConst.WoopsaNotificationQueueSize,
                        WoopsaValueType.Integer)
                });
            _remoteMethodWaitNotification = _woopsaSubscribeService.GetMethod(
                WoopsaSubscriptionServiceConst.WoopsaWaitNotification,
                WoopsaValueType.JsonData,
                new WoopsaMethodArgumentInfo[] {
                    new WoopsaMethodArgumentInfo(
                        WoopsaSubscriptionServiceConst.WoopsaSubscriptionChannel,
                        WoopsaValueType.Integer),
                    new WoopsaMethodArgumentInfo(
                        WoopsaSubscriptionServiceConst.WoopsaLastNotificationId,
                        WoopsaValueType.Integer)
                });
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

        private readonly string RegisterSubscriptionMethodPath =
            WoopsaUtils.CombinePath(WoopsaSubscriptionServiceConst.WoopsaServiceSubscriptionName,
                WoopsaSubscriptionServiceConst.WoopsaRegisterSubscription);

        private void RegisterSubscriptions(List<WoopsaClientSubscription> subscriptions)
        {
            if (_localSubscriptionService == null)
            {
                // Remote subscription using multirequest
                Dictionary<WoopsaClientSubscription, WoopsaClientRequest> requests =
                    new Dictionary<WoopsaClientSubscription, WoopsaClientRequest>();
                WoopsaClientMultiRequest multiRequest = new WoopsaClientMultiRequest();
                foreach (var item in subscriptions)
                    requests[item] = multiRequest.Invoke(RegisterSubscriptionMethodPath,
                        WoopsaSubscriptionServiceConst.RegisterSubscriptionArguments,
                        _subscriptionOpenChannel.Value, item.ServicePath,
                        item.MonitorInterval, item.PublishInterval);
                try
                {
                    _client.ExecuteMultiRequest(multiRequest);
                    // Assign the subscriptionIds
                    foreach (var item in subscriptions)
                    {
                        WoopsaClientRequest request = requests[item];
                        if (request.Result.ResultType == WoopsaClientRequestResultType.Value)
                            item.SubscriptionId = request.Result.Value;
                        item.FailedSubscription = item.SubscriptionId == null;
                        if (!item.FailedSubscription)
                            _registeredSubscriptions[item.SubscriptionId.Value] = item;
                    }
                }
                catch
                {
                }
            }
            else
            {
                // local service subscription
                foreach (var item in subscriptions)
                {
                    try
                    {
                        item.SubscriptionId = _localSubscriptionService.RegisterSubscription(
                            _subscriptionOpenChannel.Value, item.ServicePath,
                            item.MonitorInterval, item.PublishInterval);
                    }
                    catch
                    {
                    }
                    item.FailedSubscription = item.SubscriptionId == null;
                    if (!item.FailedSubscription)
                        _registeredSubscriptions[item.SubscriptionId.Value] = item;
                }
            }
        }

        private readonly string UnregisterSubscriptionMethodPath =
            WoopsaUtils.CombinePath(WoopsaSubscriptionServiceConst.WoopsaServiceSubscriptionName,
                WoopsaSubscriptionServiceConst.WoopsaUnregisterSubscription);

        private void UnregisterSubscriptions(IEnumerable<WoopsaClientSubscription> unsubscriptions)
        {
            if (_localSubscriptionService == null)
            {
                // Remote unsubscription using multirequest
                Dictionary<WoopsaClientSubscription, WoopsaClientRequest> requests =
                    new Dictionary<WoopsaClientSubscription, WoopsaClientRequest>();
                WoopsaClientMultiRequest multiRequest = new WoopsaClientMultiRequest();
                foreach (var item in unsubscriptions)
                    if (item.SubscriptionId.HasValue)
                        requests[item] = multiRequest.Invoke(UnregisterSubscriptionMethodPath,
                            WoopsaSubscriptionServiceConst.UnregisterSubscriptionArguments,
                            _subscriptionOpenChannel.Value, item.SubscriptionId);
                try
                {
                    _client.ExecuteMultiRequest(multiRequest);
                    // Remove the unsubscribed subscriptions
                    foreach (var item in unsubscriptions)
                        if (item.SubscriptionId.HasValue)
                        {
                            WoopsaClientRequest request = requests[item];
                            if (request.Result.ResultType == WoopsaClientRequestResultType.Value)
                            {
                                lock (_subscriptions)
                                {
                                    _registeredSubscriptions.Remove(item.SubscriptionId.Value);
                                    _subscriptions.Remove(item);
                                }
                                item.SubscriptionId = null;
                            }
                        }
                }
                catch
                {
                }
            }
            else
            {
                foreach (var item in unsubscriptions)
                {
                    if (item.SubscriptionId.HasValue)
                        _localSubscriptionService.UnregisterSubscription(
                            _subscriptionOpenChannel.Value, item.SubscriptionId.Value);
                    lock (_subscriptions)
                    {
                        if (item.SubscriptionId.HasValue)
                            _registeredSubscriptions.Remove(item.SubscriptionId.Value);
                        _subscriptions.Remove(item);
                    }
                    item.SubscriptionId = null;
                }
            }
        }

        [ThreadStatic]
        private static WoopsaClientSubscriptionChannel _currentChannel;

        private WoopsaClient _client;
        private WoopsaUnboundClientObject _woopsaRoot, _woopsaSubscribeService;
        private WoopsaMethod _remoteMethodCreateSubscriptionChannel,
            _remoteMethodWaitNotification;

        private int _notificationQueueSize;
        private List<WoopsaClientSubscription> _subscriptions;
        internal bool SubscriptionsChanged { get; set; }

        private Dictionary<int, WoopsaClientSubscription> _registeredSubscriptions;
        private Thread _threadNotifications, _threadSubscriptions;
        private bool _terminated;

        private object _lock;
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
            Channel.SubscriptionsChanged = true;
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
