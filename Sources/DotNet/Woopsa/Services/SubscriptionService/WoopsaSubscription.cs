using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public class WoopsaSubscription : IDisposable
    {
        public WoopsaSubscription(IWoopsaContainer container, int id, IWoopsaValue propertyLink, int monitorInterval, int publishInterval)
        {
            Id = id;
            PropertyLink = propertyLink;
            MonitorInterval = monitorInterval;
            PublishInterval = publishInterval;

            //Get the property from the link
            string server, path;
            propertyLink.DecodeWoopsaLink(out server, out path);
            if ( server == null )
            {
                var elem = container.ByPath(path);
                if (elem is IWoopsaProperty)
                {
                    _watchedProperty = elem as IWoopsaProperty;
                }
                else
                    throw new WoopsaException(String.Format("Can only create a subscription to an IWoopsaProperty. Attempted path={0}", path));
            }
            else
                throw new WoopsaException("Creating a subscription on another server is not yet supported.");

            _monitorTimer = new LightWeightTimer(monitorInterval);
            _monitorTimer.Elapsed += _monitorTimer_Elapsed;

            _publishTimer = new LightWeightTimer(publishInterval);
            _publishTimer.Elapsed += _publishTimer_Elapsed;

            _monitorTimer.Enabled = true;
            _publishTimer.Enabled = true;
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
        public int MonitorInterval { get; private set; }

        /// <summary>
        /// The Publish Interval is the minimum time between each
        /// notification between the server and the client.
        /// </summary>
        public int PublishInterval { get; private set; }

        public IWoopsaValue PropertyLink { get; private set; }

        public delegate void PublishEventHandler(object sender, PublishEventArgs e);
        public event PublishEventHandler Publish;

        public WoopsaNotification Execute()
        {
            IWoopsaValue newValue = _watchedProperty.Value;
            if (!newValue.Equals(_oldValue))
            {
                _oldValue = newValue;
                return new WoopsaNotification(new WoopsaValue(newValue.AsText, newValue.Type, DateTime.Now), PropertyLink);
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
                _monitorTimer.Dispose();
                _publishTimer.Dispose();
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
