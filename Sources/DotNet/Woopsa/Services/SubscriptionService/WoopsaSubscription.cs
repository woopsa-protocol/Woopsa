using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public class WoopsaSubscription : IDisposable
    {
        public WoopsaSubscription(WoopsaContainer container, int id, IWoopsaValue propertyLink, TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            Id = id;
            PropertyLink = propertyLink;
            MonitorInterval = monitorInterval;
            PublishInterval = publishInterval;

            //Get the property from the link
            string server, path;
            string channelContainerPath = null;
            propertyLink.DecodeWoopsaLink(out server, out path);
            if ( server == null )
            {
                // Are there any Subscription Services up to this path?
                string[] pathParts = path.TrimStart(WoopsaConst.WoopsaPathSeparator).Split(WoopsaConst.WoopsaPathSeparator);
                string searchPath = WoopsaConst.WoopsaRootPath + pathParts[0] + WoopsaConst.WoopsaPathSeparator;
                for (int i = 1; i < pathParts.Length; i++)
                {
                    if (_subscriptionChannels.ContainsKey(searchPath))
                    {
                        _subscriptionChannel = _subscriptionChannels[searchPath];
                    }
                    else
                    {
                        try
                        {
                            WoopsaObject subscriptionService = (WoopsaObject)container.ByPath(searchPath + WoopsaServiceSubscriptionConst.WoopsaServiceSubscriptionName);
                            channelContainerPath = subscriptionService.Owner.GetPath();
                            _subscriptionChannel = new WoopsaClientSubscriptionChannel((IWoopsaObject)subscriptionService.Owner);
                            break;
                        }
                        catch (WoopsaNotFoundException)
                        {
                        }
                        searchPath += pathParts[i] + WoopsaConst.WoopsaPathSeparator;
                    }
                }

                if (_subscriptionChannel == null)
                {
                    var elem = container.ByPath(path);
                    if (elem is IWoopsaProperty)
                    {
                        _watchedProperty = elem as IWoopsaProperty;
                    }
                    else
                        throw new WoopsaException(String.Format("Can only create a subscription to an IWoopsaProperty. Attempted path={0}", path));
                }
            }
            else
                throw new WoopsaException("Creating a subscription on another server is not yet supported.");

            // If we found a subscription service along the way, use that instead of polling
            if (_subscriptionChannel != null)
            {
                string subscribePath = path;
                if (subscribePath.StartsWith(channelContainerPath))
                    subscribePath = subscribePath.Substring(channelContainerPath.Length);
                _subscriptionId = _subscriptionChannel.Register(subscribePath, monitorInterval, publishInterval);
                _subscriptionChannel.ValueChange += subscriptionChannel_ValueChange;
            }
            else
            {
                _monitorTimer = new LightWeightTimer(monitorInterval);
                _monitorTimer.Elapsed += _monitorTimer_Elapsed;

                _publishTimer = new LightWeightTimer(publishInterval);
                _publishTimer.Elapsed += _publishTimer_Elapsed;

                _monitorTimer.IsEnabled = true;
                _publishTimer.IsEnabled = true;
            }
        }

        void subscriptionChannel_ValueChange(object sender, WoopsaNotificationsEventArgs e)
        {
            bool hasNotifications = false;
            foreach (IWoopsaNotification notification in e.Notifications.Notifications)
            {
                if (notification.SubscriptionId == _subscriptionId)
                {
                    _notifications.Enqueue(notification);
                    hasNotifications = true;
                }
            }
            if ( hasNotifications )
                DoPublish();
        }

        void _publishTimer_Elapsed(object sender, EventArgs e)
        {
            if ( _notifications.Count != 0 )
            {
                DoPublish();
            }
        }

        void _monitorTimer_Elapsed(object sender, EventArgs e)
        {
            var notification = Execute();
            if (notification != null)
            {
                _notifications.Enqueue(notification);
            }
        }

        /// <summary>
        /// The auto-generated Id for this Subscription
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// The Monitoring Interval is the interval at which this
        /// subscription checks the monitored value for changes.
        /// </summary>
        public TimeSpan MonitorInterval { get; private set; }

        /// <summary>
        /// The Publish Interval is the minimum time between each
        /// notification between the server and the client.
        /// </summary>
        public TimeSpan PublishInterval { get; private set; }

        public IWoopsaValue PropertyLink { get; private set; }

        public delegate void PublishEventHandler(object sender, PublishEventArgs e);
        public event PublishEventHandler Publish;

        public WoopsaNotification Execute()
        {
            IWoopsaValue newValue = _watchedProperty.Value;
            if (!newValue.Equals(_oldValue))
            {
                _oldValue = newValue;
                return new WoopsaNotification(WoopsaValue.CreateUnchecked(newValue.AsText, newValue.Type, DateTime.Now), Id);
            }
            else
            {
                return null;
            }
        }

        private IWoopsaValue _oldValue = null;
        private IWoopsaProperty _watchedProperty = null;
        private Queue<IWoopsaNotification> _notifications = new Queue<IWoopsaNotification>();
        private LightWeightTimer _monitorTimer;
        private LightWeightTimer _publishTimer;

        // This is set if we are subscribing to an inner subscription Channel
        // In the case of server daisy-chaining
        private WoopsaClientSubscriptionChannel _subscriptionChannel = null;
        private int? _subscriptionId = null;
        private static Dictionary<string, WoopsaClientSubscriptionChannel> _subscriptionChannels = new Dictionary<string, WoopsaClientSubscriptionChannel>();

        private void DoPublish()
        {
            if (Publish != null)
            {
                List<IWoopsaNotification> notificationsList = new List<IWoopsaNotification>();
                while (_notifications.Count > 0)
                {
                    IWoopsaNotification notification = _notifications.Dequeue();
                    notificationsList.Add(notification);
                }
                Publish(this, new PublishEventArgs(notificationsList));
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_subscriptionChannel != null)
                {
                    _subscriptionChannel.ValueChange -= subscriptionChannel_ValueChange;
                    _subscriptionChannel.Unregister(_subscriptionId.Value);
                    _subscriptionChannel = null;
                }
                if (_monitorTimer != null)
                {
                    _monitorTimer.Dispose();
                    _monitorTimer = null;
                }
                if (_publishTimer != null)
                { 
                    _publishTimer.Dispose();
                    _publishTimer = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public class PublishEventArgs : EventArgs
    {
        public PublishEventArgs(IEnumerable<IWoopsaNotification> notifications)
        {
            Notifications = notifications;
        }

        public IEnumerable<IWoopsaNotification> Notifications { get; private set; }
    }
}
