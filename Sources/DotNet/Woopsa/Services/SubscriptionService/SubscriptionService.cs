using System.Collections.Generic;
using System.Linq;

namespace Woopsa
{
    public class SubscriptionService : WoopsaObject, IWoopsaServiceSubscription
    {
        #region Constructors

        public SubscriptionService(WoopsaContainer container)
            : base(container, WoopsaServiceSubscriptionConst.WoopsaServiceSubscriptionName)
        {
            _createSubscriptionChannel = new WoopsaMethod(
                this,
                WoopsaServiceSubscriptionConst.WoopsaCreateSubscriptionChannel,
                WoopsaValueType.Integer,
                new WoopsaMethodArgumentInfo[] { new WoopsaMethodArgumentInfo(WoopsaServiceSubscriptionConst.WoopsaNotificationQueueSize, WoopsaValueType.Integer) },
                args => CreateSubscriptionChannel(args.First())
            );

            _registerSubscription = new WoopsaMethod(
                this,
                WoopsaServiceSubscriptionConst.WoopsaRegisterSubscription,
                WoopsaValueType.Integer,
                new WoopsaMethodArgumentInfo[] {
                    new WoopsaMethodArgumentInfo(WoopsaServiceSubscriptionConst.WoopsaSubscriptionChannel, WoopsaValueType.Integer),
                    new WoopsaMethodArgumentInfo(WoopsaServiceSubscriptionConst.WoopsaPropertyLink, WoopsaValueType.WoopsaLink),
                    new WoopsaMethodArgumentInfo(WoopsaServiceSubscriptionConst.WoopsaMonitorInterval, WoopsaValueType.TimeSpan),
                    new WoopsaMethodArgumentInfo(WoopsaServiceSubscriptionConst.WoopsaPublishInterval, WoopsaValueType.TimeSpan)
                },
                args =>
                {
                    var arguments = args as IWoopsaValue[] ?? args.ToArray();
                    return RegisterSubscription(arguments[0], arguments[1], arguments[2], arguments[3]);
                });

            _unregisterSubscription = new WoopsaMethod(
                this,
                WoopsaServiceSubscriptionConst.WoopsaUnregisterSubscription,
                WoopsaValueType.Logical,
                new WoopsaMethodArgumentInfo[] {
                    new WoopsaMethodArgumentInfo(WoopsaServiceSubscriptionConst.WoopsaSubscriptionChannel, WoopsaValueType.Integer),
                    new WoopsaMethodArgumentInfo(WoopsaServiceSubscriptionConst.WoopsaSubscriptionId, WoopsaValueType.Integer)
                },
                args =>
                {
                    var arguments = args as IWoopsaValue[] ?? args.ToArray();
                    return UnregisterSubscription(arguments[0], arguments[1]);
                });

            _waitNotification = new WoopsaMethod(
                this,
                WoopsaServiceSubscriptionConst.WoopsaWaitNotification,
                WoopsaValueType.JsonData,
                new WoopsaMethodArgumentInfo[] {
                    new WoopsaMethodArgumentInfo(WoopsaServiceSubscriptionConst.WoopsaSubscriptionChannel, WoopsaValueType.Integer),
                    new WoopsaMethodArgumentInfo(WoopsaServiceSubscriptionConst.WoopsaLastNotificationId, WoopsaValueType.Integer)
                },
                args =>
                {
                    var arguments = args as IWoopsaValue[] ?? args.ToArray();
                    return WaitNotification(arguments[0], arguments[1]);
                });
        }

        #endregion

        #region IWoopsaServiceSubscription

        public IWoopsaValue CreateSubscriptionChannel(IWoopsaValue notificationQueueSize)
        {
            var channel = new WoopsaSubscriptionChannel(notificationQueueSize.ToInt32());
            lock (_channels)
                _channels.Add(channel.Id, channel);
            return new WoopsaValue(channel.Id);
        }

        public IWoopsaValue RegisterSubscription(IWoopsaValue subscriptionChannel, IWoopsaValue woopsaPropertyLink, IWoopsaValue monitorInterval, IWoopsaValue publishInterval)
        {
            int channelId = subscriptionChannel.ToInt32();
            lock (_channels)
                if (_channels.ContainsKey(channelId))
                    return new WoopsaValue(_channels[channelId].RegisterSubscription(Container, woopsaPropertyLink, monitorInterval.ToTimeSpan(), publishInterval.ToTimeSpan()));
                else
                    throw new WoopsaException(string.Format("Tried to register a subscription on channel with id={0} that does not exist", subscriptionChannel.ToInt32()));
        }

        // Logical, true if the subscription has been found and successfully unregistered
        public IWoopsaValue UnregisterSubscription(IWoopsaValue subscriptionChannel, IWoopsaValue subscriptionId)
        {
            int channelId = subscriptionChannel.ToInt32();
            lock (_channels)
                if (_channels.ContainsKey(channelId))
                    return new WoopsaValue(_channels[channelId].UnregisterSubscription(subscriptionId.ToInt32()));
                else
                    throw new WoopsaException(string.Format("Tried to unregister a subscription on channel with id={0} that does not exist", subscriptionChannel.ToInt32()));
        }

        public IWoopsaValue WaitNotification(IWoopsaValue subscriptionChannel, IWoopsaValue lastNotificationId)
        {
            int channelId = subscriptionChannel.ToInt32();
            int notificationId = lastNotificationId.ToInt32();
            WoopsaSubscriptionChannel channel;
            lock (_channels)
                if (_channels.ContainsKey(channelId))
                    channel = _channels[channelId];
                else
                    channel = null;
            if (channel != null)
            {
                IWoopsaNotifications notifications = channel.WaitNotification(WoopsaServiceSubscriptionConst.NotificationTimeoutInterval, notificationId);
                return WoopsaValue.WoopsaJsonData(notifications.Serialize());
            }
            else
                throw new WoopsaInvalidSubscriptionChannelException(string.Format("Tried to call WaitNotification on channel with id={0} that does not exist", channelId));
        }

        #endregion

        #region Private Members

        private WoopsaMethod _createSubscriptionChannel;
        private WoopsaMethod _registerSubscription;
        private WoopsaMethod _unregisterSubscription;
        private WoopsaMethod _waitNotification;
        private readonly Dictionary<int, WoopsaSubscriptionChannel> _channels = new Dictionary<int, WoopsaSubscriptionChannel>();

        #endregion
    }
}
