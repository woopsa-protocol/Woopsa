using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public class WoopsaClientObject : WoopsaObject
    {
        internal WoopsaClientObject(WoopsaBaseClient client, WoopsaContainer container, string name, IWoopsaContainer root) 
            : base(container, name)
        {
            _client = client;
            if (root == null)
                _root = this;
            else
                _root = root;
        }

        public delegate void PropertyChanged(IWoopsaNotification value);

        public new IEnumerable<WoopsaClientProperty> Properties
        {
            get
            {
                foreach (var property in base.Properties)
                {
                    yield return (property as WoopsaClientProperty);
                }
            }
        }

        public new IEnumerable<WoopsaClientObject> Items
        {
            get
            {
                foreach (var item in base.Items)
                {
                    if ( (item as WoopsaClientObject) != null )
                        yield return (item as WoopsaClientObject);
                }
            }
        }

        public void Refresh()
        {
            Refresh(true);
        }

        private void Refresh(bool flushProperties)
        {
            _meta = _client.Meta(this.GetPath(_root));

            if (flushProperties)
            {
                // We need this apparently useless "flushProperties"
                // argument because Get'ing one of these 3 properties
                // will trigger the DoPopulate method on the WoopsaObject,
                // which will in turn ask this clientobject to call this
                // same refresh method, leading to a deadly never-ending
                // recursive call to itself!
                // However we still want to provide a way for users of 
                // this API to completely refresh this client's items.
                for (int i = Properties.Count(); i >= 0; i--)
                {
                    Remove((WoopsaProperty)Properties.ElementAt(i));
                }
                for (int i = Methods.Count(); i >= 0; i--)
                {
                    Remove((WoopsaMethod)Methods.ElementAt(i));
                }
                for (int i = Items.Count(); i >= 0; i--)
                {
                    Remove((WoopsaContainer)Items.ElementAt(i));
                }
            }

            foreach (var property in _meta.Properties)
            {
                if (property.ReadOnly)
                {
                    new WoopsaClientProperty(this,
                        property.Name,
                        (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), property.Type),
                        GetProperty);
                }
                else
                {
                    new WoopsaClientProperty(this,
                        property.Name,
                        (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), property.Type),
                        GetProperty,
                        SetProperty);
                }
            }

            foreach (var method in _meta.Methods)
            {
                List<WoopsaMethodArgumentInfo> argumentInfos = new List<WoopsaMethodArgumentInfo>();
                foreach (var argumentInfo in method.ArgumentInfos)
                    argumentInfos.Add(new WoopsaMethodArgumentInfo(argumentInfo.Name, (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), argumentInfo.Type)));
                new WoopsaMethod(this,
                    method.Name,
                    (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), method.ReturnType),
                    argumentInfos,
                    (args) => (Invoke(args, argumentInfos, method.Name)));
            }

            foreach (var item in _meta.Items)
            {
                new WoopsaClientObject(_client, this, item, _root);
            }
        }

        #region Subscription Service
        public int Subscribe(string path, PropertyChanged propertyChangedHandler, TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            // Only create the subscription channel on subscription
            // to the first property.
            if (_subscriptionChannel == null)
            {
                if (!_hasSubscriptionService.HasValue)
                {
                    _hasSubscriptionService = this.Items.ByNameOrNull(WoopsaServiceSubscriptionConst.WoopsaServiceSubscriptionName) != null;
                }
                if (_hasSubscriptionService == true)
                    _subscriptionChannel = new WoopsaClientSubscriptionChannel(this);
                else
                    _subscriptionChannel = new WoopsaClientSubscriptionChannelFallback(this);

                _subscriptionChannel.ValueChange += _subscriptionChannel_ValueChange;
            }
            int subscriptionId = _subscriptionChannel.Register(path, monitorInterval, publishInterval);
            _subscriptionsDictionary.Add(subscriptionId, propertyChangedHandler);
            return subscriptionId;
        }

        public void Unsubscribe(int id)
        {
            if (_subscriptionsDictionary.ContainsKey(id))
            {
                _subscriptionsDictionary.Remove(id);
            }
            if (_subscriptionChannel != null)
            {
                _subscriptionChannel.Unregister(id);
            }
        }

        private void _subscriptionChannel_ValueChange(object sender, WoopsaNotificationsEventArgs notifications)
        {
            foreach (var notification in notifications.Notifications.Notifications)
            {
                if (_subscriptionsDictionary.ContainsKey(notification.SubscriptionId))
                {
                    _subscriptionsDictionary[notification.SubscriptionId](notification);
                }
            }
        }
        #endregion

        protected override void PopulateObject()
        {
            base.PopulateObject();
            Refresh(false);
        }

        private WoopsaValue GetProperty(object sender)
        {
            return _client.Read(this.GetPath(_root).TrimEnd(WoopsaConst.WoopsaPathSeparator) + WoopsaConst.WoopsaPathSeparator + (sender as IWoopsaProperty).Name);
        }

        private void SetProperty(object sender, IWoopsaValue value)
        {
            _client.Write(this.GetPath(_root).TrimEnd(WoopsaConst.WoopsaPathSeparator) + WoopsaConst.WoopsaPathSeparator + (sender as IWoopsaProperty).Name, value.AsText);
        }

        private WoopsaValue Invoke(IEnumerable<IWoopsaValue> arguments, List<WoopsaMethodArgumentInfo> argumentInfos, string methodName)
        {
            NameValueCollection namedArguments = new NameValueCollection();
            for (int i = 0; i < argumentInfos.Count; i++)
            {
                namedArguments.Add(argumentInfos[i].Name, arguments.ElementAt(i).AsText);
            }
            return _client.Invoke(this.GetPath(_root).TrimEnd(WoopsaConst.WoopsaPathSeparator) + WoopsaConst.WoopsaPathSeparator + methodName, namedArguments);
        }

        private WoopsaBaseClient _client = null;
        private WoopsaMetaResult _meta = null;
        private Dictionary<int, PropertyChanged> _subscriptionsDictionary = new Dictionary<int, PropertyChanged>();

        private WoopsaClientSubscriptionChannelBase _subscriptionChannel;
        private bool? _hasSubscriptionService = null;

        private IWoopsaContainer _root;

        #region IDisposable
        protected override void Dispose(bool disposing)
        {
            _subscriptionChannel.Dispose();
        }
        #endregion
    }

    public class WoopsaClientProperty : WoopsaProperty
    {
        public WoopsaClientProperty(WoopsaClientObject container, string name, WoopsaValueType type,
            WoopsaPropertyGet get, WoopsaPropertySet set)
            : base(container, name, type, get, set) 
        {
            _container = container;
        }

        public WoopsaClientProperty(WoopsaClientObject container, string name, WoopsaValueType type,
            WoopsaPropertyGet get)
            : this( container, name, type, get, null) { }


        public static implicit operator bool(WoopsaClientProperty property)
        {
            return property.Value.ToBool();
        }

        public static implicit operator sbyte(WoopsaClientProperty property)
        {
            return property.Value.ToSByte();
        }

        public static implicit operator Int16(WoopsaClientProperty property)
        {
            return property.Value.ToInt16();
        }

        public static implicit operator Int32(WoopsaClientProperty property)
        {
            return property.Value.ToInt32();
        }

        public static implicit operator Int64(WoopsaClientProperty property)
        {
            return property.Value.ToInt64();
        }

        public static implicit operator byte(WoopsaClientProperty property)
        {
            return property.Value.ToByte();
        }

        public static implicit operator UInt16(WoopsaClientProperty property)
        {
            return property.Value.ToUInt16();
        }

        public static implicit operator UInt32(WoopsaClientProperty property)
        {
            return property.Value.ToUInt32();
        }

        public static implicit operator UInt64(WoopsaClientProperty property)
        {
            return property.Value.ToUInt64();
        }

        public static implicit operator float(WoopsaClientProperty property)
        {
            return property.Value.ToFloat();
        }

        public static implicit operator double(WoopsaClientProperty property)
        {
            return property.Value.ToDouble();
        }

        public static implicit operator DateTime(WoopsaClientProperty property)
        {
            return property.Value.ToDateTime();
        }

        public static implicit operator TimeSpan(WoopsaClientProperty property)
        {
            return property.Value.ToTimeSpan();
        }

        public static implicit operator string(WoopsaClientProperty property)
        {
            return property.Value.AsText;
        }

        public int SubscribeToChanges(EventHandler<WoopsaNotificationEventArgs> callback, TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            if (_subscriptions == null)
                _subscriptions = new Dictionary<int, EventHandler<WoopsaNotificationEventArgs>>();
            int subscriptionId = _container.Subscribe(this.GetPath(), HandlePropertyChanged, monitorInterval, publishInterval);
            lock(_subscriptions)
                _subscriptions.Add(subscriptionId, callback);
            return subscriptionId;
        }

        public void UnsubscribeToChanges(int subscriptionId)
        {
            if (_subscriptions.ContainsKey(subscriptionId))
            {
                _container.Unsubscribe(subscriptionId);
                _subscriptions.Remove(subscriptionId);
            }
        }

        public event EventHandler<WoopsaNotificationEventArgs> Change
        {
            add
            {
                if (_subscriptions == null)
                    _subscriptions = new Dictionary<int, EventHandler<WoopsaNotificationEventArgs>>();
                int subscriptionId = _container.Subscribe(this.GetPath(), HandlePropertyChanged, WoopsaServiceSubscriptionConst.DefaultMonitorInterval, WoopsaServiceSubscriptionConst.DefaultPublishInterval);
                lock(_subscriptions)
                    _subscriptions.Add(subscriptionId, value);
            }
            remove
            {
                lock (_subscriptions)
                {
                    foreach (var subscription in _subscriptions)
                    {
                        if (subscription.Value == value)
                        {
                            // First remove the Event handler
                            _subscriptions.Remove(subscription.Key);
                            // Then remove the subscription on the server
                            // if this fails, an exception will be thrown
                            _container.Unsubscribe(subscription.Key);
                        }
                    }
                }
            }
        }

        private void HandlePropertyChanged(IWoopsaNotification value)
        {
            lock (_subscriptions)
            {
                if (_subscriptions.ContainsKey(value.SubscriptionId))
                    _subscriptions[value.SubscriptionId](this, new WoopsaNotificationEventArgs(value.Value));
            }
        }

        private Dictionary<int, EventHandler<WoopsaNotificationEventArgs>> _subscriptions;
        private WoopsaClientObject _container;
    }

    public class WoopsaNotificationEventArgs : EventArgs
    {
        public WoopsaNotificationEventArgs(IWoopsaValue value)
        {
            Value = value;
        }

        public IWoopsaValue Value { get; set; }
    }

    public static class WoopsaClientExtensions
    {
        public static WoopsaClientProperty ByName(this IEnumerable<WoopsaClientProperty> properties, string name)
        {
            WoopsaClientProperty result = ByNameOrNull(properties, name);
            if (result != null)
                return result;
            else
                throw new WoopsaNotFoundException(string.Format("Woopsa property not found : {0}", name));
        }

        internal static WoopsaClientProperty ByNameOrNull(this IEnumerable<WoopsaClientProperty> properties, string name)
        {
            foreach (var item in properties)
                if (item.Name == name)
                    return item;
            return null;
        }
    }
}
