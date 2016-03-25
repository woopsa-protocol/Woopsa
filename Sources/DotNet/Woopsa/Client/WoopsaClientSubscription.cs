using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace Woopsa
{
    internal class WoopsaClientSubscriptionChannel : WoopsaClientSubscriptionChannelBase
    {
        #region Constants

        public const int DefaultNotificationQueueSize = 200;

        #endregion

        #region Constructors

        public WoopsaClientSubscriptionChannel(IWoopsaObject client)
            : this(client, DefaultNotificationQueueSize)
        {}

        public WoopsaClientSubscriptionChannel(IWoopsaObject client, int notificationQueueSize)
        {
            _client = client;
            _notificationQueueSize = notificationQueueSize;

            _subscriptionService = (IWoopsaObject)_client.Items.ByName(WoopsaServiceSubscriptionConst.WoopsaServiceSubscriptionName);
            _createSubscriptionChannel = _subscriptionService.Methods.ByName(WoopsaServiceSubscriptionConst.WoopsaCreateSubscriptionChannel);
            _waitNotification = _subscriptionService.Methods.ByName(WoopsaServiceSubscriptionConst.WoopsaWaitNotification);

            Create(_notificationQueueSize);
        }

        #endregion

        #region Public Properties

        public int ChannelId { get; private set; }

        #endregion

        #region Override Register / Unregister

        public override int Register(string path, TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            var subscription = new WoopsaClientSubscription(this, _client, path, monitorInterval, publishInterval);
            lock (_subscriptions)
            {
                // Check if we haven't already subscribed to this guy by any chance
                _subscriptions.Add(subscription);
            }

            if (_listenThread== null)
            {
                _listenThread = new Thread(DoWaitNotification) {Name = "WoopsaClientSubscription_Thread"};
                _listening = true;
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
                        if (!_subscriptions.Any())
                            _listening = false;
                        return result;
                    }
                }
            }

            return false;
        }
                
        #endregion

        #region Private Helpers

        private void Create(int notificationQueueSize)
        {
            var arguments = new List<WoopsaValue> {notificationQueueSize};
            IWoopsaValue result = _createSubscriptionChannel.Invoke(arguments);
            ChannelId = result.ToInt32();
        }

        private void DoWaitNotification()
        {
            while (_listening)
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
                        Thread.Sleep(_waitNotificationRetryPeriod);
                        Create(_notificationQueueSize);
                        lock (_subscriptions)
                        {
                            // Check if we haven't already subscribed to this guy by any chance
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
                            foreach (IWoopsaNotification notification in notifications.Notifications)
                                if (notification.Id > _lastNotificationId)
                                    _lastNotificationId = notification.Id;
                            DoValueChanged(notifications);
                        }
                    }
                    catch (WebException)
                    {
                        // There was some sort of network error. We will
                        // try again later
                        Thread.Sleep(_waitNotificationRetryPeriod);
                    }
                }
            }
        }

        private int WaitNotification(out IWoopsaNotifications notificationsResult)
        {
            return WaitNotification(out notificationsResult, _lastNotificationId);
        }

        private int WaitNotification(out IWoopsaNotifications notificationsResult, int lastNotificationId)
        {
            var arguments = new List<WoopsaValue> {ChannelId, lastNotificationId};
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

        #region Internal Nested Classes

        internal class WoopsaClientSubscription
        {
            #region Constructors

            public WoopsaClientSubscription(WoopsaClientSubscriptionChannel channel, IWoopsaObject client, string path, TimeSpan monitorInterval, TimeSpan publishInterval)
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
                var arguments = new List<WoopsaValue> {_channel.ChannelId, Id};
                IWoopsaValue result = _unregisterSubscription.Invoke(arguments);
                return result.ToBool();
            }

            #endregion

            #region Private Helpers

            private int Register(TimeSpan monitorInterval, TimeSpan publishInterval)
            {
                var arguments = new List<WoopsaValue>();
                arguments.Add(_channel.ChannelId);
                arguments.Add(WoopsaValue.CreateUnchecked(Path, WoopsaValueType.WoopsaLink));
                arguments.Add(monitorInterval);
                arguments.Add(publishInterval);
                IWoopsaValue result = _registerSubscription.Invoke(arguments);
                return result.ToInt32();
            }

            #endregion

            #region Private Members

            private readonly WoopsaClientSubscriptionChannel _channel;
            private readonly IWoopsaObject _client;
            private readonly IWoopsaObject _subscriptionService;
            private readonly IWoopsaMethod _registerSubscription;
            private readonly IWoopsaMethod _unregisterSubscription;
            private readonly TimeSpan _monitorInterval;
            private readonly TimeSpan _publishInterval;

            #endregion
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _listening = false;
            }
        }

        #endregion

        #region Private Members

        private readonly IWoopsaObject _client;
        private readonly List<WoopsaClientSubscription> _subscriptions = new List<WoopsaClientSubscription>();
        private bool _listening;
        private Thread _listenThread;

        private IWoopsaObject _subscriptionService;
        private readonly IWoopsaMethod _createSubscriptionChannel;
        private readonly IWoopsaMethod _waitNotification;
        private readonly int _notificationQueueSize;

        private int _lastNotificationId;

        private readonly TimeSpan _waitNotificationRetryPeriod = TimeSpan.FromSeconds(10);

        #endregion
    }
}
