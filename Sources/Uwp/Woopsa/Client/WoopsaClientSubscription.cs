using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Woopsa
{
    internal class WoopsaClientSubscriptionChannel : WoopsaClientSubscriptionChannelBase, IDisposable
    {
        public const int DefaultNotificationQueueSize = 200;
        private readonly TimeSpan WaitNotificationTimeout = TimeSpan.FromSeconds(10);
        private readonly TimeSpan WaitNotificationRetryPeriod = TimeSpan.FromSeconds(10);

        public int ChannelId { get; private set; }

        public WoopsaClientSubscriptionChannel(IWoopsaObject client) : this(client, DefaultNotificationQueueSize) { }

        public WoopsaClientSubscriptionChannel(IWoopsaObject client, int notificationQueueSize)
        {
            _client = client;
            _notificationQueueSize = notificationQueueSize;

            _subscriptionService = (IWoopsaObject)_client.Items.ByName(WoopsaServiceSubscriptionConst.WoopsaServiceSubscriptionName);
            _createSubscriptionChannel = (IWoopsaMethod)_subscriptionService.Methods.ByName(WoopsaServiceSubscriptionConst.WoopsaCreateSubscriptionChannel);
            _waitNotification = (IWoopsaMethod)_subscriptionService.Methods.ByName(WoopsaServiceSubscriptionConst.WoopsaWaitNotification);

            Create(_notificationQueueSize);
        }

        public override int Register(string path, TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            WoopsaClientSubscription subscription = new WoopsaClientSubscription(this, _client, path, monitorInterval, publishInterval);
            lock (_subscriptions)
            {
                //Check if we haven't already subscribed to this guy by any chance
                _subscriptions.Add(subscription);
            }
            if (!_listenStarted)
            {
                _listenTask = new Task(DoWaitNotification);
                //_listenTask.Name = "WoopsaClientSubscription_Thread";
                _listenStarted = true;
                _listenTask.Start();
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
                            _listenStarted = false;
                        return result;
                    }
                }
            }
            return false;
        }

        private IWoopsaObject _client;
        private List<WoopsaClientSubscription> _subscriptions = new List<WoopsaClientSubscription>();
        private bool _listenStarted = false;
        private Task _listenTask;

        private IWoopsaObject _subscriptionService;
        private IWoopsaMethod _createSubscriptionChannel;
        private IWoopsaMethod _waitNotification;
        private int _notificationQueueSize;

        private int _lastNotificationId = 0;

        private void DoWaitNotification()
        {
            while (_listenStarted)
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
                        Task.Delay(WaitNotificationRetryPeriod);
                        Create(_notificationQueueSize);
                        lock (_subscriptions)
                        {
                            //Check if we haven't already subscribed to this guy by any chance
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
                        Task.Delay(WaitNotificationRetryPeriod);
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
            List<WoopsaValue> arguments = new List<WoopsaValue>();
            arguments.Add(ChannelId);
            arguments.Add(lastNotificationId);
            IWoopsaValue val = _waitNotification.Invoke(arguments);
            WoopsaJsonData results = ((WoopsaValue)val).JsonData;
            WoopsaNotifications notificationsList = new WoopsaNotifications();
            int count = 0;
            for (int i = 0; i < results.Length; i++ )
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

        private void Create(int notificationQueueSize)
        {
            List<WoopsaValue> arguments = new List<WoopsaValue>();
            arguments.Add(notificationQueueSize);
            IWoopsaValue result = _createSubscriptionChannel.Invoke(arguments);
            ChannelId = result.ToInt32();
        }

        internal class WoopsaClientSubscription
        {
            public WoopsaClientSubscription(WoopsaClientSubscriptionChannel channel, IWoopsaObject client, string path, TimeSpan monitorInterval, TimeSpan publishInterval)
            {
                _channel = channel;
                _client = client;
                _monitorInterval = monitorInterval;
                _publishInterval = publishInterval;
                _subscriptionService = (IWoopsaObject)_client.Items.ByName(WoopsaServiceSubscriptionConst.WoopsaServiceSubscriptionName);
                _registerSubscription = (IWoopsaMethod)_subscriptionService.Methods.ByName(WoopsaServiceSubscriptionConst.WoopsaRegisterSubscription);
                _unregisterSubscription = (IWoopsaMethod)_subscriptionService.Methods.ByName(WoopsaServiceSubscriptionConst.WoopsaUnregisterSubscription);
                Path = path;
                Register();
            }
            
            public void Register()
            {
                Id = Register(_monitorInterval, _publishInterval);
            }


            public int Id { get; private set; }
            public string Path { get; private set; }

            public bool Unregister()
            {
                List<WoopsaValue> arguments = new List<WoopsaValue>();
                arguments.Add(_channel.ChannelId);
                arguments.Add(Id);
                IWoopsaValue result = _unregisterSubscription.Invoke(arguments);
                return result.ToBool();
            }

            private WoopsaClientSubscriptionChannel _channel;
            private IWoopsaObject _client;
            private IWoopsaObject _subscriptionService;
            private IWoopsaMethod _registerSubscription;
            private IWoopsaMethod _unregisterSubscription;
            private TimeSpan _monitorInterval;
            private TimeSpan _publishInterval;

            private int Register(TimeSpan monitorInterval, TimeSpan publishInterval)
            {
                List<WoopsaValue> arguments = new List<WoopsaValue>();
                arguments.Add(_channel.ChannelId);
                arguments.Add(WoopsaValue.CreateUnchecked(Path, WoopsaValueType.WoopsaLink));
                arguments.Add(monitorInterval);
                arguments.Add(publishInterval); 
                IWoopsaValue result = _registerSubscription.Invoke(arguments);
                return result.ToInt32();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _listenStarted = false;
            }
        }
    }
}
