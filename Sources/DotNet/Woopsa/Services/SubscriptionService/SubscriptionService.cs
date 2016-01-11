using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public class SubscriptionService : WoopsaObject, IWoopsaServiceSubscription
    {
        public SubscriptionService(WoopsaContainer container)
            : base(container, WoopsaServiceSubscriptionConst.WoopsaServiceSubscriptionName)
        { 
            _createSubscriptionChannel = new WoopsaMethod(
                this, 
                WoopsaServiceSubscriptionConst.WoopsaCreateSubscriptionChannel,
                WoopsaValueType.Integer,
                new WoopsaMethodArgumentInfo[]{new WoopsaMethodArgumentInfo(WoopsaServiceSubscriptionConst.WoopsaNotificationQueueSize, WoopsaValueType.Integer)},
                (args) => (this.CreateSubscriptionChannel(args.First()))
            );

            _registerSubscription = new WoopsaMethod(
                this,
                WoopsaServiceSubscriptionConst.WoopsaRegisterSubscription,
                WoopsaValueType.Integer,
                new WoopsaMethodArgumentInfo[]{
                    new WoopsaMethodArgumentInfo(WoopsaServiceSubscriptionConst.WoopsaSubscriptionChannel, WoopsaValueType.Integer),
                    new WoopsaMethodArgumentInfo(WoopsaServiceSubscriptionConst.WoopsaPropertyLink, WoopsaValueType.WoopsaLink),
                    new WoopsaMethodArgumentInfo(WoopsaServiceSubscriptionConst.WoopsaMonitorInterval, WoopsaValueType.TimeSpan),
                    new WoopsaMethodArgumentInfo(WoopsaServiceSubscriptionConst.WoopsaPublishInterval, WoopsaValueType.TimeSpan)
                },
                (args) => (RegisterSubscription(args.ElementAt(0), args.ElementAt(1), args.ElementAt(2), args.ElementAt(3)))
            );

            _unregisterSubscription = new WoopsaMethod(
                this,
                WoopsaServiceSubscriptionConst.WoopsaUnregisterSubscription,
                WoopsaValueType.Logical,
                new WoopsaMethodArgumentInfo[]{
                    new WoopsaMethodArgumentInfo(WoopsaServiceSubscriptionConst.WoopsaSubscriptionChannel, WoopsaValueType.Integer),
                    new WoopsaMethodArgumentInfo(WoopsaServiceSubscriptionConst.WoopsaSubscriptionId, WoopsaValueType.Integer)
                },
                (args) => (UnregisterSubscription(args.ElementAt(0), args.ElementAt(1)))
            );

            _waitNotification = new WoopsaMethod(
                this,
                WoopsaServiceSubscriptionConst.WoopsaWaitNotification,
                WoopsaValueType.JsonData,
                new WoopsaMethodArgumentInfo[]{
                    new WoopsaMethodArgumentInfo(WoopsaServiceSubscriptionConst.WoopsaSubscriptionChannel, WoopsaValueType.Integer),
                    new WoopsaMethodArgumentInfo(WoopsaServiceSubscriptionConst.WoopsaLastNotificationId, WoopsaValueType.Integer)
                },
                (args) => (WaitNotification(args.ElementAt(0), args.ElementAt(1)))
            );
        }

        public IWoopsaValue CreateSubscriptionChannel(IWoopsaValue notificationQueueSize)
        {
            WoopsaSubscriptionChannel channel = new WoopsaSubscriptionChannel(notificationQueueSize.ToInt32());
            _channels.Add(channel.Id, channel);
            return new WoopsaValue(channel.Id);
        }

        public IWoopsaValue RegisterSubscription(IWoopsaValue subscriptionChannel, IWoopsaValue woopsaPropertyLink, IWoopsaValue monitorInterval, IWoopsaValue publishInterval)
        {
            int channelId = subscriptionChannel.ToInt32();
            if (_channels.ContainsKey(channelId))
            {
                return new WoopsaValue(_channels[channelId].RegisterSubscription(Container, woopsaPropertyLink, monitorInterval.ToTimeSpan(), publishInterval.ToTimeSpan()));
            }
            else
                throw new WoopsaException(String.Format("Tried to register a subscription on channel with id={0} that does not exist", subscriptionChannel.ToInt32()));
        }

        // Logical, true if the subsciption has been found and successfully unregistered
        public IWoopsaValue UnregisterSubscription(IWoopsaValue subscriptionChannel, IWoopsaValue subscriptionId)
        {
            int channelId = subscriptionChannel.ToInt32();
            if (_channels.ContainsKey(channelId))
            {
                return new WoopsaValue(_channels[channelId].UnregisterSubscription(subscriptionId.ToInt32()));
            }
            else
                throw new WoopsaException(String.Format("Tried to uregister a subscription on channel with id={0} that does not exist", subscriptionChannel.ToInt32()));
        }

        public IWoopsaValue WaitNotification(IWoopsaValue subscriptionChannel, IWoopsaValue lastNotificationId)
        {
            int channelId = subscriptionChannel.ToInt32();
            int notificationId = lastNotificationId.ToInt32();
            if (_channels.ContainsKey(channelId))
            {
                IWoopsaNotifications notifications = _channels[channelId].WaitNotification(WoopsaServiceSubscriptionConst.NotificationTimeoutInterval, notificationId);
                return WoopsaValue.WoopsaJsonData(notifications.Serialize());
            }
            else
                throw new WoopsaInvalidSubscriptionChannelException(String.Format("Tried to call WaitNotification on channel with id={0} that does not exist", channelId));
        }
            
        private WoopsaMethod _createSubscriptionChannel;
        private WoopsaMethod _registerSubscription;
        private WoopsaMethod _unregisterSubscription;
        private WoopsaMethod _waitNotification;

        private Dictionary<int, WoopsaSubscriptionChannel> _channels = new Dictionary<int, WoopsaSubscriptionChannel>();
    }
}
