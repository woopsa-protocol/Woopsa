using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Woopsa
{
    public class WoopsaClientSubscriptionChannel
    {
        #region Constructor

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
            _subscriptionLock = new SemaphoreSlim(1, 1);
            _lostSubscriptions = new List<int>();
        }

        #endregion

        #region Static fields

        public static readonly TimeSpan ReconnectionInterval = TimeSpan.FromMilliseconds(100);

        #endregion

        #region Constants / Readonly

        public const int MaxSubscriptionsPerMultiRequest = 50;
        public const int MaxUnsubscriptionsPerMultiRequest = 250;

        private readonly string RegisterSubscriptionMethodPath =
            WoopsaUtils.CombinePath(WoopsaSubscriptionServiceConst.WoopsaServiceSubscriptionName,
            WoopsaSubscriptionServiceConst.WoopsaRegisterSubscription);

        private readonly string UnregisterSubscriptionMethodPath =
            WoopsaUtils.CombinePath(WoopsaSubscriptionServiceConst.WoopsaServiceSubscriptionName,
                WoopsaSubscriptionServiceConst.WoopsaUnregisterSubscription);

        #endregion

        #region Internal Methods
        internal WoopsaClientSubscription Subscribe(string servicePath, string relativePath,
             EventHandler<WoopsaNotificationEventArgs> handler,
             TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            WoopsaClientSubscription subscription =
                new WoopsaClientSubscription(this, servicePath, relativePath, monitorInterval, publishInterval, handler);

            lock (_subscriptions)
                _subscriptions.Add(subscription);
            SubscriptionsChanged = true;

            EnsureServiceTaskStarted();
            return subscription;
        }

        #endregion

        #region Public Methods

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
            RequestAllUnregistration();
            _terminated = true;
            if (_localSubscriptionService != null)
                _localSubscriptionService.Terminate();
        }

        #endregion

        #region Fields / Attributes

        private WoopsaClient _client;
        private WoopsaUnboundClientObject _woopsaRoot, _woopsaSubscribeService;
        private WoopsaMethod _remoteMethodCreateSubscriptionChannel, _remoteMethodWaitNotification;

        private int _notificationQueueSize;
        private List<WoopsaClientSubscription> _subscriptions;

        private Dictionary<int, WoopsaClientSubscription> _registeredSubscriptions;

        private Task _taskNotifications, _taskSubscriptions;
        private bool _terminated;

        private object _channelLock;
        private readonly SemaphoreSlim _subscriptionLock;
        private int? _subscriptionOpenChannel;
        private WoopsaSubscriptionServiceImplementation _localSubscriptionService;
        private int _lastNotificationId;
        private List<int> _lostSubscriptions;

        private static AsyncLocal<WoopsaClientSubscriptionChannel> _currentChannel = new AsyncLocal<WoopsaClientSubscriptionChannel>();

        #endregion

        #region Properties


        internal int SubscriptionsCount
        {
            get
            {
                lock (_subscriptions)
                {
                    return _subscriptions.Count;
                }
            }
        }

        internal bool SubscriptionsChanged { get; set; }

        internal int RegisteredSubscriptionCount
        {
            get
            {
                _subscriptionLock.Wait();
                try
                {
                    return _registeredSubscriptions.Count;
                }
                finally
                {
                    _subscriptionLock.Release();
                }
            }
        }

        /// <summary>
        /// returns the current subscription channel in which we are executing.
        /// return null if the current context is not a thread of the subscription channel.
        /// </summary>
        public static WoopsaClientSubscriptionChannel CurrentChannel => _currentChannel.Value;

        public bool Terminated => _terminated;

        #endregion

        #region Private Methods

        private async Task<bool> UnregisterSubscriptionsAsync(IEnumerable<WoopsaClientSubscription> unsubscriptions)
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
                    await _client.ExecuteMultiRequestAsync(multiRequest);
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

        private async Task<bool> UnregisterLostSubscriptionsAsync(IEnumerable<int> lostSubscriptionIds)
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
                    await _client.ExecuteMultiRequestAsync(multiRequest);
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

        private void EnsureServiceTaskStarted()
        {
            if (_taskNotifications == null)
            {
                _taskNotifications = new Task(async () => await ExecuteServiceNotificationsAsync());
                _taskNotifications.Start();
            }
            if (_taskSubscriptions == null)
            {
                _taskSubscriptions = new Task(async () => await ExecuteServiceSubscriptionsAsync());
                _taskSubscriptions.Start();
            }
        }

        private async Task ExecuteServiceNotificationsAsync()
        {
            _currentChannel.Value = this;
            while (!_terminated)
                try
                {
                    if (_subscriptionOpenChannel == null)
                    {
                        await OpenChannelAsync();
                    }
                    else
                    {
                        bool hasSubscriptions;
                        lock (_subscriptions)
                            hasSubscriptions = _subscriptions.Count > 0;
                        if (hasSubscriptions)
                            await ProcessNotificationsAsync();
                        else
                            await Task.Delay(TimeSpan.FromMilliseconds(1));
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
                        await Task.Delay(ReconnectionInterval);
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

        private async Task ExecuteServiceSubscriptionsAsync()
        {
            _currentChannel.Value = this;
            while (!_terminated)
            {
                try
                {
                    if (_subscriptionOpenChannel != null)
                        if (SubscriptionsChanged)
                            await ManageSubscriptionsAsync();
                }
                catch (Exception)
                {
                }
                // do not manage too quickly the subscriptions update to improve 
                // grouping of subscriptions into a single multirequest
                await Task.Delay(WoopsaSubscriptionServiceConst.ClientSubscriptionUpdateInterval);
            }
        }

        private async Task OpenChannelAsync()
        {
            try
            {
                _subscriptionOpenChannel = await CreateSubscriptionChannelAsync(_notificationQueueSize);
            }
            catch (WoopsaNotFoundException)
            {
                // No subscription service available, create a local one
                _localSubscriptionService = new WoopsaSubscriptionServiceImplementation(
                        _woopsaRoot, false);
                try
                {
                    _subscriptionOpenChannel = await CreateSubscriptionChannelAsync(_notificationQueueSize);
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

        private async Task ManageSubscriptionsAsync()
        {
            await ManageNewUnsubscriptionsAsync();
            await ManageNewSubscriptionsAsync();
            await ManageLostSubscriptionsAsync();
        }

        private async Task ManageNewUnsubscriptionsAsync()
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
                {
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
                }
                if (newUnsubscriptions != null)
                    if (!await UnregisterSubscriptionsAsync(newUnsubscriptions))
                        break;
            }
            while (newUnsubscriptions != null);
        }

        private async Task ManageNewSubscriptionsAsync()
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
                    if (!await RegisterSubscriptionsAsync(newSubscriptions))
                        break;
            }
            while (newSubscriptions != null);
        }

        private async Task ManageLostSubscriptionsAsync()
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
                    if (!await UnregisterLostSubscriptionsAsync(newLostUnsubscriptions))
                        break;
            }
            while (newLostUnsubscriptions != null);
        }

        private async Task ProcessNotificationsAsync()
        {
            Tuple<int, WoopsaClientNotifications> tuple;
            try
            {
                tuple = await RetrieveNotificationAsync(_lastNotificationId);
                if (tuple.Item1 > 0)
                    ExecuteNotifications(tuple.Item2);
            }
            catch (WoopsaNotificationsLostException)
            {
                // This happens when we haven't "collected" notifications
                // fast enough and the queue has been filled up completely,
                // forcing past notifications to be erased. We must 
                // acknowledge to the server.
                tuple = await RetrieveNotificationAsync(0);
                if (tuple.Item1 > 0)
                    ExecuteNotifications(tuple.Item2);
            }
        }

        private async Task<Tuple<int, WoopsaClientNotifications>> RetrieveNotificationAsync(int lastNotificationId)
        {
            WoopsaJsonData results = await WaitNotificationAsync(_subscriptionOpenChannel.Value, lastNotificationId);
            var notificationsList = new WoopsaClientNotifications();

            int count = 0;
            for (int i = 0; i < results.Length; i++)
            {
                WoopsaJsonData notification = results[i];
                var notificationValue = notification["Value"];
                var notificationSubscriptionId = notification["SubscriptionId"];
                var notificationId = notification["Id"];
                WoopsaValueType type = (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), notificationValue["Type"]);
                WoopsaValue value = WoopsaValue.CreateChecked(notificationValue["Value"],
                    type, DateTime.Parse(notificationValue["TimeStamp"].AsText, CultureInfo.InvariantCulture));
                WoopsaClientNotification newNotification = new WoopsaClientNotification(value, notificationSubscriptionId);
                newNotification.Id = notificationId;
                notificationsList.Add(newNotification);
                count++;
            }
            return new Tuple<int, WoopsaClientNotifications>(count, notificationsList);
        }

        private void ExecuteNotifications(WoopsaClientNotifications notifications)
        {
            List<int> lostSubscriptions = null;
            // Synchronize with the notification registration process to be sure we will not
            // process notifications for subscriptions that are not yet completely registered
            _subscriptionLock.Wait();
            try
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
            finally
            {
                _subscriptionLock.Release();
            }
            if (lostSubscriptions != null)
                lock (_lostSubscriptions)
                    _lostSubscriptions.AddRange(lostSubscriptions);
        }

        private async Task<bool> RegisterSubscriptionsAsync(List<WoopsaClientSubscription> subscriptions)
        {
            if (_localSubscriptionService == null)
            {
                // Prepare remote subscription using multirequest
                Dictionary<WoopsaClientSubscription, WoopsaClientRequest> requests =
                    new Dictionary<WoopsaClientSubscription, WoopsaClientRequest>();
                WoopsaClientMultiRequest multiRequest = new WoopsaClientMultiRequest();
                try
                {
                    foreach (var item in subscriptions)
                    requests[item] = multiRequest.Invoke(RegisterSubscriptionMethodPath,
                        WoopsaSubscriptionServiceConst.RegisterSubscriptionArguments,
                        _subscriptionOpenChannel.Value, item.ServicePath,
                        item.MonitorInterval, item.PublishInterval);

                    await _subscriptionLock.WaitAsync();
                    try
                    {
                        // Prevent receiving new notifications until the new subscriptions are all registered
                        // Otherwise, we will not be able to dispatch properly notifications coming
                        // from those new subscriptions
                        await _client.ExecuteMultiRequestAsync(multiRequest);
                        // Assign the subscriptionIds
                        foreach (var item in subscriptions)
                        {
                            WoopsaClientRequest request = requests[item];

                            // Detect if the channel (_subscriptionOpenChannel.Value) is obsolete and close it in order to open a new channel.
                            // Can occurs when server restarts
                            if (request.Result.Error != null && request.Result.Error is WoopsaInvalidSubscriptionChannelException)
                            {
                                CloseChannel();
                                return false;
                            }

                            if (request.Result.ResultType == WoopsaClientRequestResultType.Value)
                                item.SubscriptionId = request.Result.Value;
                            item.FailedSubscription = item.SubscriptionId == null;
                            if (!item.FailedSubscription)
                                _registeredSubscriptions[item.SubscriptionId.Value] = item;
                        }
                    }
                    finally
                    {
                        _subscriptionLock.Release();
                    }              
                    return true;
                }
                catch (Exception)
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

            //_remoteMethodCreateSubscriptionChannelAsync = _woopsaSubscribeService.GetAsynchronousMethod(
            //    WoopsaSubscriptionServiceConst.WoopsaCreateSubscriptionChannelAsync,
            //    WoopsaValueType.Integer,
            //    new WoopsaMethodArgumentInfo[] {
            //        new WoopsaMethodArgumentInfo(
            //            WoopsaSubscriptionServiceConst.WoopsaNotificationQueueSize,
            //            WoopsaValueType.Integer)
            //    });

            //_remoteMethodWaitNotificationAsync = _woopsaSubscribeService.GetAsynchronousMethod(
            //    WoopsaSubscriptionServiceConst.WoopsaWaitNotificationAsync,
            //    WoopsaValueType.JsonData,
            //    new WoopsaMethodArgumentInfo[] {
            //        new WoopsaMethodArgumentInfo(
            //            WoopsaSubscriptionServiceConst.WoopsaSubscriptionChannel,
            //            WoopsaValueType.Integer),
            //        new WoopsaMethodArgumentInfo(
            //            WoopsaSubscriptionServiceConst.WoopsaLastNotificationId,
            //            WoopsaValueType.Integer)
            //    });
        }

        private async Task<int> CreateSubscriptionChannelAsync(int notificationQueueSize)
        {
            if (_localSubscriptionService == null)
                return await _remoteMethodCreateSubscriptionChannel.InvokeAsync(notificationQueueSize);
            else
                return _localSubscriptionService.CreateSubscriptionChannel(notificationQueueSize);
        }

        private async Task<WoopsaJsonData> WaitNotificationAsync(int subscriptionChannel, int lastNotificationId)
        {
            if (_localSubscriptionService == null)
                return (await _remoteMethodWaitNotification.InvokeAsync(subscriptionChannel, lastNotificationId)).JsonData;
            else
                return _localSubscriptionService.WaitNotification(subscriptionChannel, lastNotificationId);
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

        #endregion

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _terminated = true;
                CloseChannel();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
