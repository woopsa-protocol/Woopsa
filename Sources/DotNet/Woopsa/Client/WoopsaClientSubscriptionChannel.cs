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
            _channelLock = new object();
            _subscriptionLock = new object();
            _lostSubscriptions = new List<int>();
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

        public const int MaxSubscriptionsPerMultiRequest = 100;
        public const int MaxUnsubscriptionsPerMultiRequest = 500;

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
            TimeSpan timeout = WoopsaSubscriptionServiceConst.TimeOutUnsubscriptionPerSubscription.Multiply(_registeredSubscriptions.Count);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            while (_registeredSubscriptions.Count > 0)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(1));
                if (watch.Elapsed > timeout)
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
            {
                try
                {
                    lock (_channelLock)
                        if (_subscriptionOpenChannel != null)
                            if (SubscriptionsChanged)
                                ManageSubscriptions();
                }
                catch (Exception)
                {
                }
                // do not manage too quickly the subscriptions update to improve 
                // grouping of subscriptions into a single multirequest
                Thread.Sleep(WoopsaSubscriptionServiceConst.ClientSubscriptionUpdateInterval);
            }
        }

        private void OpenChannel()
        {
            lock (_channelLock)
            {
                try
                {
                    _subscriptionOpenChannel = CreateSubscriptionChannel(_notificationQueueSize);
                }
                catch (WoopsaNotFoundException)
                {
                    // No subscription service available, create a local one
                    _localSubscriptionService = new WoopsaSubscriptionServiceImplementation(
                         _woopsaRoot, false);
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
            lock (_channelLock)
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
            ManageNewUnsubscriptions();
            ManageNewSubscriptions();
            ManageLostSubscriptions();
        }

        private void ManageNewUnsubscriptions()
        {
            // New unsubscriptions
            List<WoopsaClientSubscription> newUnsubscriptions;
            // Unsubscription can be unsuccessful (for example when the server is down)
            // So process every unsubscription only once to avoid staying in this loop
            HashSet<WoopsaClientSubscription> processedSubscriptions = null;
            do
            {
                if (_terminated)
                    break;
                newUnsubscriptions = null;
                lock (_subscriptions)
                    foreach (var item in _subscriptions)
                        if (item.UnsubscriptionRequested)
                        {
                            if (processedSubscriptions == null)
                                processedSubscriptions = new HashSet<WoopsaClientSubscription>();
                            if (!processedSubscriptions.Contains(item))
                            {
                                processedSubscriptions.Add(item);
                                if (newUnsubscriptions == null)
                                    newUnsubscriptions = new List<WoopsaClientSubscription>();
                                newUnsubscriptions.Add(item);
                                if (newUnsubscriptions.Count >= MaxUnsubscriptionsPerMultiRequest)
                                    break;
                            }
                        }
                if (newUnsubscriptions != null)
                    if (!UnregisterSubscriptions(newUnsubscriptions))
                        break;
            }
            while (newUnsubscriptions != null);
        }

        private void ManageNewSubscriptions()
        {
            // New subscriptions
            List<WoopsaClientSubscription> newSubscriptions;
            SubscriptionsChanged = false;
            do
            {
                if (_terminated)
                    break;
                newSubscriptions = null;
                lock (_subscriptions)
                    foreach (var item in _subscriptions)
                        if (item.SubscriptionId == null)
                        {
                            if (newSubscriptions == null)
                                newSubscriptions = new List<WoopsaClientSubscription>();
                            newSubscriptions.Add(item);
                            if (newSubscriptions.Count >= MaxSubscriptionsPerMultiRequest)
                                break;
                        }
                if (newSubscriptions != null)
                    if (!RegisterSubscriptions(newSubscriptions))
                        break;
            }
            while (newSubscriptions != null);
        }

        private void ManageLostSubscriptions()
        {
            // Unsubscribe lost subscriptions
            List<int> newLostUnsubscriptions;
            HashSet<int> processedLostSubscriptions = null;
            do
            {
                if (_terminated)
                    break;
                newLostUnsubscriptions = null;
                lock (_lostSubscriptions)
                    foreach (var item in _lostSubscriptions)
                    {
                        if (processedLostSubscriptions == null)
                            processedLostSubscriptions = new HashSet<int>();
                        if (!processedLostSubscriptions.Contains(item))
                        {
                            processedLostSubscriptions.Add(item);
                            if (newLostUnsubscriptions == null)
                                newLostUnsubscriptions = new List<int>();
                            newLostUnsubscriptions.Add(item);
                            if (newLostUnsubscriptions.Count >= MaxUnsubscriptionsPerMultiRequest)
                                break;
                        }
                    }
                if (newLostUnsubscriptions != null)
                    if (!UnregisterLostSubscriptions(newLostUnsubscriptions))
                        break;
            }
            while (newLostUnsubscriptions != null);
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
            List<int> lostSubscriptions = null;
            // Synchronize with the notification registration process to be sure we will not
            // process notifications for subscriptions that are not yet completely registered
            lock (_subscriptionLock)
            {
                foreach (WoopsaClientNotification notification in notifications.Notifications)
                {
                    if (_registeredSubscriptions.ContainsKey(notification.SubscriptionId))
                        _registeredSubscriptions[notification.SubscriptionId].Execute(notification);
                    else
                    {
                        // there is a lost Subscription in the server which produces notifications, we have to unsubscribe it
                        if (lostSubscriptions == null)
                            lostSubscriptions = new List<int>();
                        lostSubscriptions.Add(notification.SubscriptionId);
                    }
                    if (notification.Id > _lastNotificationId)
                        _lastNotificationId = notification.Id;
                }
            }
            if (lostSubscriptions != null)
                lock (_lostSubscriptions)
                    _lostSubscriptions.AddRange(lostSubscriptions);
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

        private bool RegisterSubscriptions(List<WoopsaClientSubscription> subscriptions)
        {
            if (_localSubscriptionService == null)
            {
                // Prepare remote subscription using multirequest
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
                    // Prevent receiving new notifications until the new subscriptions are all registered
                    // Otherwise, we will not be able to dispatch properly notifications coming
                    // from those new subscriptions
                    lock (_subscriptionLock)
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
                    return true;
                }
                catch
                {
                    return false;
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
                return true;
            }
        }

        private readonly string UnregisterSubscriptionMethodPath =
            WoopsaUtils.CombinePath(WoopsaSubscriptionServiceConst.WoopsaServiceSubscriptionName,
                WoopsaSubscriptionServiceConst.WoopsaUnregisterSubscription);

        private bool UnregisterSubscriptions(IEnumerable<WoopsaClientSubscription> unsubscriptions)
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
                    return true;
                }
                catch
                {
                    return false;
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
                return true;
            }
        }

        private bool UnregisterLostSubscriptions(IEnumerable<int> lostSubscriptionIds)
        {
            if (_localSubscriptionService == null)
            {
                // Remote unsubscription using multirequest
                Dictionary<int, WoopsaClientRequest> requests =
                    new Dictionary<int, WoopsaClientRequest>();
                WoopsaClientMultiRequest multiRequest = new WoopsaClientMultiRequest();
                foreach (var item in lostSubscriptionIds)
                    requests[item] = multiRequest.Invoke(UnregisterSubscriptionMethodPath,
                        WoopsaSubscriptionServiceConst.UnregisterSubscriptionArguments,
                        _subscriptionOpenChannel.Value, item);
                try
                {
                    _client.ExecuteMultiRequest(multiRequest);
                    // Remove the unsubscribed subscriptions
                    foreach (var item in lostSubscriptionIds)
                    {
                        WoopsaClientRequest request = requests[item];
                        if (request.Result.ResultType == WoopsaClientRequestResultType.Value)
                            lock (_lostSubscriptions)
                                _lostSubscriptions.Remove(item);
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                foreach (var item in lostSubscriptionIds)
                {
                    lock (_lostSubscriptions)
                        _lostSubscriptions.Remove(item);
                }
                return true;
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

        private object _channelLock;
        private object _subscriptionLock;
        private int? _subscriptionOpenChannel;
        private WoopsaSubscriptionServiceImplementation _localSubscriptionService;
        private int _lastNotificationId;
        private List<int> _lostSubscriptions;
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
                try
                {
                    Handler(Channel, new WoopsaNotificationEventArgs(notification, this));
                }
                catch (Exception)
                {
                    // TODO : how to manage exceptions thrown by notification handlers ?
                }
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
