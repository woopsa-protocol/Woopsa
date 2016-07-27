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

        public WoopsaClientSubscriptionChannel(WoopsaUnboundClientObject woopsaRoot,
            int notificationQueueSize)
        {
            _woopsaRoot = woopsaRoot;
            _woopsaSubscribeService = _woopsaRoot.GetUnboundItem(
                WoopsaServiceSubscriptionConst.WoopsaServiceSubscriptionName);
            CreateWoopsaSubscriptionServiceMethods();
            _notificationQueueSize = notificationQueueSize;
            _subscriptions = new List<WoopsaClientSubscription>();
            _registeredSubscriptions = new Dictionary<int, WoopsaClientSubscription>();
        }

        public WoopsaClientSubscription Subscribe(string path,
            TimeSpan monitorInterval, TimeSpan publishInterval,
            EventHandler<WoopsaNotificationEventArgs> handler)
        {
            return Subscribe(path, path, monitorInterval, publishInterval, handler);
        }

        internal WoopsaClientSubscription Subscribe(string servicePath, string relativePath,
             TimeSpan monitorInterval, TimeSpan publishInterval,
             EventHandler<WoopsaNotificationEventArgs> handler)
        {
            WoopsaClientSubscription subscription =
                new WoopsaClientSubscription(this, servicePath, relativePath, monitorInterval, publishInterval, handler);
            lock (_subscriptions)
                _subscriptions.Add(subscription);
            EnsureServiceThreadStarted();
            return subscription;
        }

        internal void Unsubscribe(WoopsaClientSubscription woopsaClientSubscription)
        {
            _methodUnregisterSubscription.Invoke(_subscriptionOpenChannel,
                woopsaClientSubscription.SubscriptionId);
            woopsaClientSubscription.SubscriptionId = null;
            lock (_subscriptions)
            {
                if (woopsaClientSubscription.SubscriptionId.HasValue)
                    _registeredSubscriptions.Remove(woopsaClientSubscription.SubscriptionId.Value);
                _subscriptions.Remove(woopsaClientSubscription);
            }
        }

        private void EnsureServiceThreadStarted()
        {
            if (_thread == null)
            {
                _thread = new Thread(new ThreadStart(executeService));
                _thread.Start();
            }
        }

        private void executeService()
        {
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
                catch (WebException e)
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
                catch (Exception e)
                {
                }
        }

        private void OpenChannel()
        {
            try
            {
                _subscriptionOpenChannel = _methodCreateSubscriptionChannel.Invoke(_notificationQueueSize);
            }
            catch (WoopsaNotFoundException)
            {
                // no subscription service available, create a local one
                _localSubscriptionService = new SubscriptionService(_woopsaRoot);
                _subscriptionOpenChannel = _methodCreateSubscriptionChannel.Invoke(_notificationQueueSize);
                //TODO : détecter la perte de connection du service de souscription et fermer le canal
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
                    item.SubscriptionId = _methodRegisterSubscription.Invoke(
                        _subscriptionOpenChannel, item.ServicePath, item.MonitorInterval, item.PublishInterval);
                    _registeredSubscriptions[item.SubscriptionId.Value] = item;
                }
        }

        private void ProcessNotifications()
        {
            IWoopsaNotifications notifications;
            try
            {
                if (WaitNotification(out notifications, _lastNotificationId) > 0)
                    ExecuteNotifications(notifications);
            }
            catch (WoopsaNotificationsLostException)
            {
                // This happens when we haven't "collected" notifications
                // fast enough and the queue has been filled up completely,
                // forcing past notifications to be erased. We must 
                // acknowledge to the server.
                if (WaitNotification(out notifications, 0) > 0)
                    ExecuteNotifications(notifications);
            }
        }

        private int WaitNotification(out IWoopsaNotifications notificationsResult, long lastNotificationId)
        {
            var arguments = new WoopsaValue[] { _subscriptionOpenChannel, lastNotificationId };
            IWoopsaValue val = _methodWaitNotification.Invoke(arguments);
            WoopsaJsonData results = ((WoopsaValue)val).JsonData;

            var notificationsList = new WoopsaNotifications();

            int count = 0;
            for (int i = 0; i < results.Length; i++)
            {
                WoopsaJsonData notification = results[i];
                var notificationValue = notification["Value"];
                var notificationSubscriptionId = notification["SubscriptionId"];
                var notificationId = notification["Id"];
                WoopsaValueType type = (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), notificationValue["Type"]);
                WoopsaValue value = WoopsaValue.CreateChecked(notificationValue["Value"], type, DateTime.Parse(notificationValue["TimeStamp"]));
                WoopsaNotification newNotification = new WoopsaNotification(value, notificationSubscriptionId);
                newNotification.Id = notificationId;
                notificationsList.Add(newNotification);
                count++;
            }
            notificationsResult = notificationsList;
            return count;
        }

        private void ExecuteNotifications(IWoopsaNotifications notifications)
        {
            foreach (IWoopsaNotification notification in notifications.Notifications)
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
                if (_thread != null)
                {
                    _thread.Join();
                    _thread = null;
                }
                CloseChannel();
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
            _methodCreateSubscriptionChannel = _woopsaSubscribeService.GetMethod(
                WoopsaServiceSubscriptionConst.WoopsaCreateSubscriptionChannel,
                new WoopsaMethodArgumentInfo[] {
                    new WoopsaMethodArgumentInfo(
                        WoopsaServiceSubscriptionConst.WoopsaNotificationQueueSize,
                        WoopsaValueType.Integer)
                },
                WoopsaValueType.Integer);
            _methodRegisterSubscription = _woopsaSubscribeService.GetMethod(
                WoopsaServiceSubscriptionConst.WoopsaRegisterSubscription,
                new WoopsaMethodArgumentInfo[] {
                    new WoopsaMethodArgumentInfo(
                        WoopsaServiceSubscriptionConst.WoopsaSubscriptionChannel,
                        WoopsaValueType.Integer),
                    new WoopsaMethodArgumentInfo(
                        WoopsaServiceSubscriptionConst.WoopsaPropertyLink,
                        WoopsaValueType.WoopsaLink),
                    new WoopsaMethodArgumentInfo(
                        WoopsaServiceSubscriptionConst.WoopsaMonitorInterval,
                        WoopsaValueType.TimeSpan),
                    new WoopsaMethodArgumentInfo(
                        WoopsaServiceSubscriptionConst.WoopsaPublishInterval,
                        WoopsaValueType.TimeSpan)
                },
                WoopsaValueType.Integer);
            _methodUnregisterSubscription = _woopsaSubscribeService.GetMethod(
                WoopsaServiceSubscriptionConst.WoopsaUnregisterSubscription,
                new WoopsaMethodArgumentInfo[] {
                    new WoopsaMethodArgumentInfo(
                        WoopsaServiceSubscriptionConst.WoopsaSubscriptionChannel,
                        WoopsaValueType.Integer),
                    new WoopsaMethodArgumentInfo(
                        WoopsaServiceSubscriptionConst.WoopsaSubscriptionId,
                        WoopsaValueType.Integer)
                },
                WoopsaValueType.Integer);
            _methodWaitNotification = _woopsaSubscribeService.GetMethod(
                WoopsaServiceSubscriptionConst.WoopsaWaitNotification,
                new WoopsaMethodArgumentInfo[] {
                    new WoopsaMethodArgumentInfo(
                        WoopsaServiceSubscriptionConst.WoopsaSubscriptionChannel,
                        WoopsaValueType.Integer),
                    new WoopsaMethodArgumentInfo(
                        WoopsaServiceSubscriptionConst.WoopsaLastNotificationId,
                        WoopsaValueType.Integer)
                },
                WoopsaValueType.JsonData);
        }

        private WoopsaUnboundClientObject _woopsaRoot, _woopsaSubscribeService;
        private WoopsaMethod _methodCreateSubscriptionChannel, _methodWaitNotification,
            _methodRegisterSubscription, _methodUnregisterSubscription;

        private int _notificationQueueSize;
        private List<WoopsaClientSubscription> _subscriptions;
        private Dictionary<int, WoopsaClientSubscription> _registeredSubscriptions;
        private Thread _thread;
        private bool _terminated;

        private int? _subscriptionOpenChannel;
        private SubscriptionService _localSubscriptionService;
        private long _lastNotificationId;
    }

    public class WoopsaClientSubscription
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
            Channel.Unsubscribe(this);
        }

        internal void Execute(IWoopsaNotification notification)
        {
            if (Handler != null)
                Handler(Channel, new WoopsaNotificationEventArgs(notification, this));
        }

        public bool IsSubscribed { get { return SubscriptionId != null; } }

        public TimeSpan MonitorInterval { get; private set; }
        public TimeSpan PublishInterval { get; private set; }
        public string ServicePath { get; private set; }
        public string RelativePath { get; private set; }
        public EventHandler<WoopsaNotificationEventArgs> Handler { get; private set; }
        public WoopsaClientSubscriptionChannel Channel { get; private set; }

        internal int? SubscriptionId { get; set; }
    }




    ///////////////////////////////////////////////////////////////////////


    /*
        internal class WoopsaClientSubscriptionChannelOLD : WoopsaClientSubscriptionChannelBase
        {
            #region Constants

            public const int DefaultNotificationQueueSize = 10000;

            #endregion

            #region Constructors

            public WoopsaClientSubscriptionChannelOLD(IWoopsaObject client)
                : this(client, DefaultNotificationQueueSize)
            { }

            public WoopsaClientSubscriptionChannelOLD(IWoopsaObject client, int notificationQueueSize)
            {
                _client = client;
                _subscriptions = new List<WoopsaClientSubscriptionOLD>();
                _notificationQueueSize = notificationQueueSize;

                _subscriptionService = (IWoopsaObject)_client.Items.ByName(WoopsaServiceSubscriptionConst.WoopsaServiceSubscriptionName);
                _createSubscriptionChannel = _subscriptionService.Methods.ByName(WoopsaServiceSubscriptionConst.WoopsaCreateSubscriptionChannel);
                _waitNotification = _subscriptionService.Methods.ByName(WoopsaServiceSubscriptionConst.WoopsaWaitNotification);

                Create(_notificationQueueSize);
            }

            #endregion

            #region Public Properties

            public int? ChannelId { get; private set; }

            #endregion

            #region Override Register / Unregister

            public override int Register(string path, TimeSpan monitorInterval, TimeSpan publishInterval)
            {
                var subscription = new WoopsaClientSubscriptionOLD(this, _client, path, monitorInterval, publishInterval);
                lock (_subscriptions)
                {
                    _subscriptions.Add(subscription);
                }

                if (_listenThread == null)
                {
                    _listenThread = new Thread(DoWaitNotification) { Name = "WoopsaClientSubscription_Thread" };
                    _listenThread.Start();
                }

                return subscription.Id;
            }

            public override bool Unregister(int id)
            {
                lock (_subscriptions)
                {
                    foreach (var subscription in _subscriptions)
                    {
                        if (subscription.Id.Equals(id))
                        {
                            bool result = subscription.Unregister();
                            _subscriptions.Remove(subscription);
                            return result;
                        }
                    }
                }

                return false;
            }

            public override void Terminate()
            {
                _terminate = true;
            }


            #endregion

            #region Private Helpers

            private void Create(int notificationQueueSize)
            {
                var arguments = new WoopsaValue[] { notificationQueueSize };
                IWoopsaValue result = _createSubscriptionChannel.Invoke(arguments);
                ChannelId = result.ToInt32();
            }

            private void DoWaitNotification()
            {
                while (!_terminate)
                {
                    if (_subscriptions.Count > 0)
                    {
                        IWoopsaNotifications notifications;
                        try
                        {
                            if (WaitNotification(out notifications) > 0)
                            {
                                foreach (IWoopsaNotification notification in notifications.Notifications)
                                    if (notification.Id > _lastNotificationId)
                                        _lastNotificationId = notification.Id;
                                DoValueChanged(notifications);
                            }
                        }
                        catch (WoopsaInvalidSubscriptionChannelException)
                        {
                            // This can happen if the server died and was relaunched
                            // => the channel doesn't exist anymore so we need to re-
                            // create it and re-subscribe
                            Thread.Sleep(WaitNotificationRetryPeriod);
                            Create(_notificationQueueSize);
                            lock (_subscriptions)
                            {
                                // TODO: Check if we haven't already subscribed to this guy by any chance
                                foreach (var sub in _subscriptions)
                                    sub.Register();
                            }
                            _lastNotificationId = 1;
                        }
                        catch (WoopsaNotificationsLostException)
                        {
                            // This happens when we haven't "collected" notifications
                            // fast enough and the queue has been filled up completely,
                            // forcing past notifications to be erased. We must 
                            // acknowledge to the server.
                            if (WaitNotification(out notifications, 0) > 0)
                            {
                                // TODO analyse this code : may fail
                                foreach (IWoopsaNotification notification in notifications.Notifications)
                                    if (notification.Id > _lastNotificationId)
                                        _lastNotificationId = notification.Id;
                                DoValueChanged(notifications);
                            }
                        }
                        catch (WoopsaException)
                        {

                        }
                        catch (WebException e)
                        {
                            if (!_terminate)
                            {
                                // There was some sort of network error. We will
                                // try again later
                                Thread.Sleep(WaitNotificationRetryPeriod);
                            }
                            else
                            {
                                // Cancelled WebRequest, ignore
                            }
                        }
                        catch (ObjectDisposedException)
                        {

                        }
                    }
                    else
                        Thread.Sleep(1);
                }
            }

            private int WaitNotification(out IWoopsaNotifications notificationsResult)
            {
                return WaitNotification(out notificationsResult, _lastNotificationId);
            }

            private int WaitNotification(out IWoopsaNotifications notificationsResult, long lastNotificationId)
            {
                var arguments = new WoopsaValue[] { ChannelId, lastNotificationId };
                IWoopsaValue val = _waitNotification.Invoke(arguments);
                WoopsaJsonData results = ((WoopsaValue)val).JsonData;

                var notificationsList = new WoopsaNotifications();

                int count = 0;
                for (int i = 0; i < results.Length; i++)
                {
                    WoopsaJsonData notification = results[i];
                    var notificationValue = notification["Value"];
                    var notificationSubscriptionId = notification["SubscriptionId"];
                    var notificationId = notification["Id"];
                    WoopsaValueType type = (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), notificationValue["Type"]);
                    WoopsaValue value = WoopsaValue.CreateChecked(notificationValue["Value"], type, DateTime.Parse(notificationValue["TimeStamp"]));
                    WoopsaNotification newNotification = new WoopsaNotification(value, notificationSubscriptionId);
                    newNotification.Id = notificationId;
                    notificationsList.Add(newNotification);
                    count++;
                }

                notificationsResult = notificationsList;
                return count;
            }

            #endregion

            #region IDisposable

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (disposing)
                {
                    if (_listenThread != null)
                    {
                        _listenThread.Join();
                        _listenThread = null;
                    }
                }
            }

            #endregion

            #region Private Members

            private readonly IWoopsaObject _client;
            private readonly List<WoopsaClientSubscriptionOLD> _subscriptions;
            private bool _terminate;
            private Thread _listenThread;

            private IWoopsaObject _subscriptionService;
            private readonly IWoopsaMethod _createSubscriptionChannel;
            private readonly IWoopsaMethod _waitNotification;
            private readonly int _notificationQueueSize;

            private long _lastNotificationId;

            private readonly TimeSpan WaitNotificationRetryPeriod = TimeSpan.FromMilliseconds(200);

            #endregion
        }

        internal class WoopsaClientSubscriptionOLD
        {
            #region Constructors

            public WoopsaClientSubscriptionOLD(WoopsaClientSubscriptionChannelOLD channel, IWoopsaObject client, string path, TimeSpan monitorInterval, TimeSpan publishInterval)
            {
                _channel = channel;
                _client = client;
                _monitorInterval = monitorInterval;
                _publishInterval = publishInterval;
                _subscriptionService = (IWoopsaObject)_client.Items.ByName(WoopsaServiceSubscriptionConst.WoopsaServiceSubscriptionName);
                _registerSubscription = _subscriptionService.Methods.ByName(WoopsaServiceSubscriptionConst.WoopsaRegisterSubscription);
                _unregisterSubscription = _subscriptionService.Methods.ByName(WoopsaServiceSubscriptionConst.WoopsaUnregisterSubscription);
                Path = path;
                Register();
            }

            #endregion

            #region Public Properties

            public int Id { get; private set; }

            public string Path { get; private set; }

            #endregion

            #region Public Methods

            public void Register()
            {
                Id = Register(_monitorInterval, _publishInterval);
            }

            public bool Unregister()
            {
                IWoopsaValue result = _unregisterSubscription.Invoke(new WoopsaValue[]
                    { _channel.ChannelId, Id});
                return result.ToBool();
            }

            #endregion

            #region Private Helpers

            private int Register(TimeSpan monitorInterval, TimeSpan publishInterval)
            {
                IWoopsaValue result = _registerSubscription.Invoke(
                    new WoopsaValue[] {_channel.ChannelId,
                            WoopsaValue.WoopsaRelativeLink(Path), monitorInterval, publishInterval});
                return result.ToInt32();
            }

            #endregion

            #region Private Members

            private readonly WoopsaClientSubscriptionChannelOLD _channel;
            private readonly IWoopsaObject _client;
            private readonly IWoopsaObject _subscriptionService;
            private readonly IWoopsaMethod _registerSubscription;
            private readonly IWoopsaMethod _unregisterSubscription;
            private readonly TimeSpan _monitorInterval;
            private readonly TimeSpan _publishInterval;

            #endregion
        }
    */
    public class WoopsaNotificationEventArgs : EventArgs
    {
        public WoopsaNotificationEventArgs(IWoopsaNotification notification,
            WoopsaClientSubscription subscription)
        {
            Notification = notification;
            Subscription = subscription;
        }

        public IWoopsaNotification Notification { get; private set; }
        public WoopsaClientSubscription Subscription { get; private set; }
    }


    // TODO : Enlever ?
    public class WoopsaNotificationsEventArgs : EventArgs
    {
        public WoopsaNotificationsEventArgs(IWoopsaNotifications notifications)
        {
            Notifications = notifications;
        }

        public IWoopsaNotifications Notifications { get; private set; }

    }


}
