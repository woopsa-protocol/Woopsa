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
            EventHandler<WoopsaNotificationEventArgs> handler,
            TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            return Subscribe(path, path, handler, monitorInterval, publishInterval);
        }

        public bool Terminated { get { return _terminated; } }

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
                _thread.Name = "WoopsaClientScubscription";
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
                _subscriptionOpenChannel = _methodCreateSubscriptionChannel.Invoke(_notificationQueueSize);
            }
            catch (WoopsaNotFoundException)
            {
                // TODO : this does not work. make local calls to the localSubscriptionService !
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
            WoopsaClientNotifications notifications;
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

        private int WaitNotification(out WoopsaClientNotifications notificationsResult, long lastNotificationId)
        {
            var arguments = new WoopsaValue[] { _subscriptionOpenChannel, lastNotificationId };
            IWoopsaValue val = _methodWaitNotification.Invoke(arguments);
            WoopsaJsonData results = ((WoopsaValue)val).JsonData;

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

        [ThreadStatic]
        private static WoopsaClientSubscriptionChannel _currentChannel;

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

        internal void Execute(WoopsaClientNotification notification)
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
