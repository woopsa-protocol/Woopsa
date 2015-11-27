using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Woopsa
{
    internal class WoopsaClientSubscriptionChannel : WoopsaClientSubscriptionChannelBase
    {
        public const int DefaultNotificationQueueSize = 200;
        private readonly TimeSpan WaitNotificationTimeout = TimeSpan.FromSeconds(10);

        public int ChannelId { get; private set; }

        public WoopsaClientSubscriptionChannel(IWoopsaObject client) : this(client, DefaultNotificationQueueSize) { }

        public WoopsaClientSubscriptionChannel(IWoopsaObject client, int notificationQueueSize)
        {
            _client = client;

            _subscriptionService = (IWoopsaObject)_client.Items.ByName(WoopsaServiceSubscriptionConst.WoopsaServiceSubscriptionName);
            _createSubscriptionChannel = (IWoopsaMethod)_subscriptionService.Methods.ByName(WoopsaServiceSubscriptionConst.WoopsaCreateSubscriptionChannel);
            _waitNotification = (IWoopsaMethod)_subscriptionService.Methods.ByName(WoopsaServiceSubscriptionConst.WoopsaWaitNotification);

            Register(notificationQueueSize);
        }

        public override void Register(string path)
        {
            Register(path, WoopsaClientSubscription.DefaultMonitorInterval, WoopsaClientSubscription.DefaultPublishInterval);
        }

        public override void Register(string path, TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            WoopsaClientSubscription subscription = new WoopsaClientSubscription(this, _client, path, monitorInterval, publishInterval);
            lock (_subscriptions)
            {
                //Check if we haven't already subscribed to this guy by any chance
                foreach (var sub in _subscriptions)
                    if (sub.Path.Equals(path))
                        return;
                _subscriptions.Add(subscription);
            }
            if (!_listenStarted)
            {
                _listenThread = new Thread(DoWaitNotification);
                _listenThread.Name = "WoopsaClientSubscription_Thread";
                _listenThread.Start();
                _listenStarted = true;
            }
        }

        public override bool Unregister(string path)
        {
            lock (_subscriptions)
            {
                foreach (var subscription in _subscriptions)
                {
                    if (subscription.Path.Equals(path))
                    {
                        bool result = subscription.Unregister();
                        _subscriptions.Remove(subscription);
                        return result;
                    }
                }
            }
            return false;
        }

        private IWoopsaObject _client;
        private List<WoopsaClientSubscription> _subscriptions = new List<WoopsaClientSubscription>();
        private bool _listenStarted = false;
        private Thread _listenThread;

        private IWoopsaObject _subscriptionService;
        private IWoopsaMethod _createSubscriptionChannel;
        private IWoopsaMethod _waitNotification;


        private void DoWaitNotification()
        {
            while (true)
            {
                if (_subscriptions.Count > 0)
                {
                    IWoopsaNotifications notifications;
                    if (WaitNotification(out notifications) > 0)
                    {
                        DoValueChanged(notifications);
                    }
                }
            }
        }

        private int WaitNotification(out IWoopsaNotifications notificationsResult)
        {
            List<WoopsaValue> arguments = new List<WoopsaValue>();
            arguments.Add(ChannelId);
            IWoopsaValue val = _waitNotification.Invoke(arguments);
            WoopsaJsonData results = ((WoopsaValue)val).JsonData;
            //WoopsaJsonData results = _client.Invoke(WoopsaConst.WoopsaRootPath + SubscriptionService + WoopsaServiceSubscriptionConst.WoopsaWaitNotification, arguments, (int)WaitNotificationTimeout.TotalMilliseconds).JsonData;
            WoopsaNotifications notificationsList = new WoopsaNotifications();
            int count = 0;
            for (int i = 0; i < results.Length; i++ )
            {
                WoopsaJsonData notification = results[i];
                var notificationValue = notification["Value"];
                var notificationPropertyLink = notification["PropertyLink"];
                WoopsaValueType type = (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), notificationValue["Type"]);
                WoopsaValue value = new WoopsaValue(notificationValue["Value"], type, DateTime.Parse(notificationValue["TimeStamp"]));
                WoopsaValue propertyLink = new WoopsaValue(notificationPropertyLink["Value"], WoopsaValueType.WoopsaLink);
                WoopsaNotification newNotification = new WoopsaNotification(value, propertyLink);
                notificationsList.Add(newNotification);
                count++;
            }
            notificationsResult = notificationsList;
            return count;
        }

        private void Register(int notificationQueueSize)
        {
            List<WoopsaValue> arguments = new List<WoopsaValue>();
            arguments.Add(notificationQueueSize);
            IWoopsaValue result = _createSubscriptionChannel.Invoke(arguments);
            //IWoopsaValue result = _client.Invoke(WoopsaConst.WoopsaRootPath + SubscriptionService + WoopsaServiceSubscriptionConst.WoopsaCreateSubscriptionChannel, arguments);
            ChannelId = result.ToInt32();
        }

        private class WoopsaClientSubscription
        {
            public static readonly TimeSpan DefaultMonitorInterval = TimeSpan.FromMilliseconds(200);
            public static readonly TimeSpan DefaultPublishInterval = TimeSpan.FromMilliseconds(200);

            public WoopsaClientSubscription(WoopsaClientSubscriptionChannel channel, IWoopsaObject client, string path)
                : this(channel, client, path, DefaultMonitorInterval, DefaultPublishInterval) { }

            public WoopsaClientSubscription(WoopsaClientSubscriptionChannel channel, IWoopsaObject client, string path, TimeSpan monitorInterval, TimeSpan publishInterval)
            {
                _channel = channel;
                _client = client;
                _subscriptionService = (IWoopsaObject)_client.Items.ByName(WoopsaServiceSubscriptionConst.WoopsaServiceSubscriptionName);
                _registerSubscription = (IWoopsaMethod)_subscriptionService.Methods.ByName(WoopsaServiceSubscriptionConst.WoopsaRegisterSubscription);
                _unregisterSubscription = (IWoopsaMethod)_subscriptionService.Methods.ByName(WoopsaServiceSubscriptionConst.WoopsaUnregisterSubscription);
                Path = path;
                Id = Register(monitorInterval, publishInterval);
            }

            public int Id { get; private set; }
            public string Path { get; private set; }

            public bool Unregister()
            {
                List<WoopsaValue> arguments = new List<WoopsaValue>();
                arguments.Add(Id);
                IWoopsaValue result = _unregisterSubscription.Invoke(arguments);
                //IWoopsaValue result = _client.Invoke(WoopsaServiceSubscriptionConst.WoopsaUnregisterSubscription, arguments);
                return result.ToBool();
            }

            private WoopsaClientSubscriptionChannel _channel;
            private IWoopsaObject _client;
            private IWoopsaObject _subscriptionService;
            private IWoopsaMethod _registerSubscription;
            private IWoopsaMethod _unregisterSubscription;

            private int Register(TimeSpan monitorInterval, TimeSpan publishInterval)
            {
                List<WoopsaValue> arguments = new List<WoopsaValue>();
                arguments.Add(_channel.ChannelId);
                arguments.Add(new WoopsaValue(Path, WoopsaValueType.WoopsaLink));
                arguments.Add(monitorInterval);
                arguments.Add(publishInterval); 
                IWoopsaValue result = _registerSubscription.Invoke(arguments);
                //IWoopsaValue result = _client.Invoke(WoopsaConst.WoopsaRootPath + SubscriptionService + WoopsaServiceSubscriptionConst.WoopsaRegisterSubscription, arguments);
                return result.ToInt32();
            }
        }
    }
}
