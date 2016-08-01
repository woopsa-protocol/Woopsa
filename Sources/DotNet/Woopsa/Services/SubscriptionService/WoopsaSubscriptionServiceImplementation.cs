using System;
using System.Collections.Generic;
using System.Linq;

namespace Woopsa
{
    internal class WoopsaSubscriptionServiceImplementation : IWoopsaSubscriptionService, IDisposable
    {
        public WoopsaSubscriptionServiceImplementation(WoopsaContainer root, bool isServerSide)
        {
            _root = root;
            _isServerSide = isServerSide;
            _channels = new Dictionary<int, WoopsaSubscriptionChannel>();
            _timerCheckChannelTimedOut = new LightWeightTimer(WoopsaSubscriptionServiceConst.SubscriptionChannelLifeTimeCheckInterval);
            _timerCheckChannelTimedOut.Elapsed += _timerCheckChannelTimedOut_Elapsed;
            _timerCheckChannelTimedOut.IsEnabled = true;
        }

        public void Refresh()
        {
            foreach (var item in _channels.Values)
                item.Refresh();
        }
        /// <summary>
        /// This event is triggered before ServiceImplementation accesses the WoopsaObject model.
        /// It is usefull to protect against concurrent WoopsaObject accesses at a global level.
        /// </summary>
        public event EventHandler BeforeWoopsaModelAccess;

        /// <summary>
        /// This event is triggered after ServiceImplementation accesses the WoopsaObject model.
        /// It is guaranteed that for each BeforeWoopsaModelAccess event fired, 
        /// the AfterWoopsaModelAccess event will be fired.
        /// </summary>
        public event EventHandler AfterWoopsaModelAccess;

        internal protected virtual void OnBeforeWoopsaModelAccess()
        {
            if (BeforeWoopsaModelAccess != null)
                BeforeWoopsaModelAccess(this, new EventArgs());
        }

        internal protected virtual void OnAfterWoopsaModelAccess()
        {
            if (AfterWoopsaModelAccess != null)
                AfterWoopsaModelAccess(this, new EventArgs());
        }


        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_timerCheckChannelTimedOut != null)
                {
                    _timerCheckChannelTimedOut.Dispose();
                    _timerCheckChannelTimedOut = null;
                }
                if (_channels != null)
                {
                    foreach (var item in _channels.Values)
                        item.Dispose();
                    _channels = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region IWoopsaServiceSubscription

        public int CreateSubscriptionChannel(int notificationQueueSize)
        {
            var channel = new WoopsaSubscriptionChannel(notificationQueueSize);
            channel.BeforeWoopsaModelAccess += Channel_BeforeWoopsaModelAccess;
            channel.AfterWoopsaModelAccess += Channel_AfterWoopsaModelAccess;
            lock (_channels)
                _channels.Add(channel.Id, channel);
            return new WoopsaValue(channel.Id);
        }

        public int RegisterSubscription(int subscriptionChannelId, string woopsaPropertyPath, TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            int channelId = subscriptionChannelId;
            lock (_channels)
                if (_channels.ContainsKey(channelId))
                    return _channels[channelId].RegisterSubscription(_root, _isServerSide,
                        woopsaPropertyPath, monitorInterval, publishInterval);
                else
                    throw new WoopsaException(string.Format("Tried to register a subscription on channel with id={0} that does not exist", subscriptionChannelId));
        }

        // Logical, true if the subscription has been found and successfully unregistered
        public bool UnregisterSubscription(int subscriptionChannel, int subscriptionId)
        {
            int channelId = subscriptionChannel;
            lock (_channels)
                if (_channels.ContainsKey(channelId))
                    return _channels[channelId].UnregisterSubscription(subscriptionId);
                else
                    // if the channel does not exist anymore, unregistration is considered successful
                    return true;
        }

        public WoopsaJsonData WaitNotification(int subscriptionChannel, int lastNotificationId)
        {
            int channelId = subscriptionChannel;
            int notificationId = lastNotificationId;
            WoopsaSubscriptionChannel channel;
            lock (_channels)
                if (_channels.ContainsKey(channelId))
                    channel = _channels[channelId];
                else
                    channel = null;
            if (channel != null)
            {
                IWoopsaNotifications notifications = channel.WaitNotification(WoopsaSubscriptionServiceConst.WaitNotificationTimeout, notificationId);
                return WoopsaJsonData.CreateFromText(notifications.Serialize());
            }
            else
                throw new WoopsaInvalidSubscriptionChannelException(string.Format("Tried to call WaitNotification on channel with id={0} that does not exist", channelId));
        }

        #endregion

        #region Private Members

        private void Channel_BeforeWoopsaModelAccess(object sender, EventArgs e)
        {
            OnBeforeWoopsaModelAccess();
        }

        private void Channel_AfterWoopsaModelAccess(object sender, EventArgs e)
        {
            OnAfterWoopsaModelAccess();
        }

        private void _timerCheckChannelTimedOut_Elapsed(object sender, EventArgs e)
        {
            WoopsaSubscriptionChannel[] timedoutChannels;
            lock (_channels)
            {
                timedoutChannels = _channels.Values.Where((item) => item.ClientTimedOut).ToArray();
            }
            foreach (var item in timedoutChannels)
            {
                lock (_channels)
                    _channels.Remove(item.Id);
                item.Dispose();
            }
        }

        private Dictionary<int, WoopsaSubscriptionChannel> _channels;
        private LightWeightTimer _timerCheckChannelTimedOut;
        private WoopsaContainer _root;
        private bool _isServerSide;

        #endregion
    }
}
