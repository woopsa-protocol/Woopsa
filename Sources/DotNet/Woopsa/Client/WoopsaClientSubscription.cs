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
        private const string SubscriptionService = "SubscriptionService/";

        public int ChannelId { get; private set; }

        public WoopsaClientSubscriptionChannel(WoopsaBaseClient client) : this(client, DefaultNotificationQueueSize) { }

        public WoopsaClientSubscriptionChannel(WoopsaBaseClient client, int notificationQueueSize)
        {
            _client = client;
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

        private WoopsaBaseClient _client;
        private List<WoopsaClientSubscription> _subscriptions = new List<WoopsaClientSubscription>();
        private bool _listenStarted = false;
        private Thread _listenThread;

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
            NameValueCollection arguments = new NameValueCollection();
            arguments.Add(WoopsaServiceSubscriptionConst.WoopsaSubscriptionChannel, ChannelId.ToString());
            WoopsaJsonData results = _client.Invoke(WoopsaConst.WoopsaRootPath + SubscriptionService + WoopsaServiceSubscriptionConst.WoopsaWaitNotification, arguments, (int)WaitNotificationTimeout.TotalMilliseconds).JsonData;
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
            NameValueCollection arguments = new NameValueCollection();
            arguments.Add(WoopsaServiceSubscriptionConst.WoopsaNotificationQueueSize, notificationQueueSize.ToString());
            IWoopsaValue result = _client.Invoke(WoopsaConst.WoopsaRootPath + SubscriptionService + WoopsaServiceSubscriptionConst.WoopsaCreateSubscriptionChannel, arguments);
            ChannelId = result.ToInt32();
        }

        private class WoopsaClientSubscription
        {
            public static readonly TimeSpan DefaultMonitorInterval = TimeSpan.FromMilliseconds(200);
            public static readonly TimeSpan DefaultPublishInterval = TimeSpan.FromMilliseconds(200);

            public WoopsaClientSubscription(WoopsaClientSubscriptionChannel channel, WoopsaBaseClient client, string path)
                : this(channel, client, path, DefaultMonitorInterval, DefaultPublishInterval) { }

            public WoopsaClientSubscription(WoopsaClientSubscriptionChannel channel, WoopsaBaseClient client, string path, TimeSpan monitorInterval, TimeSpan publishInterval)
            {
                _channel = channel;
                _client = client;
                Path = path;
                Id = Register(monitorInterval, publishInterval);
            }

            public int Id { get; private set; }
            public string Path { get; private set; }

            public bool Unregister()
            {
                NameValueCollection arguments = new NameValueCollection();
                arguments.Add(WoopsaServiceSubscriptionConst.WoopsaSubscriptionId, Id.ToString());
                IWoopsaValue result = _client.Invoke(WoopsaServiceSubscriptionConst.WoopsaUnregisterSubscription, arguments);
                return result.ToBool();
            }

            private WoopsaClientSubscriptionChannel _channel;
            private WoopsaBaseClient _client;

            private int Register(TimeSpan monitorInterval, TimeSpan publishInterval)
            {
                NameValueCollection arguments = new NameValueCollection();
                arguments.Add(WoopsaServiceSubscriptionConst.WoopsaSubscriptionChannel, _channel.ChannelId.ToString());
                arguments.Add(WoopsaServiceSubscriptionConst.WoopsaPropertyLink, Path);
                arguments.Add(WoopsaServiceSubscriptionConst.WoopsaMonitorInterval, monitorInterval.TotalSeconds.ToStringWoopsa());
                arguments.Add(WoopsaServiceSubscriptionConst.WoopsaPublishInterval, publishInterval.TotalSeconds.ToStringWoopsa());
                IWoopsaValue result = _client.Invoke(WoopsaConst.WoopsaRootPath + SubscriptionService + WoopsaServiceSubscriptionConst.WoopsaRegisterSubscription, arguments);
                return result.ToInt32();
            }
        }
    }
}
