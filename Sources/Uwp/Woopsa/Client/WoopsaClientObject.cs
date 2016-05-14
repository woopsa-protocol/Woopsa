using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Woopsa
{
    public class WoopsaClientObject : WoopsaObject
    {
        #region Constructors

        internal WoopsaClientObject(WoopsaBaseClient client, WoopsaContainer container, string name, IWoopsaContainer root)
            : base(container, name)
        {
            _client = client;
            _root = root ?? this;
        }

        #endregion

        #region Public Delegates

        public delegate void PropertyChanged(object sender, IWoopsaNotification notification);

        #endregion

        #region Public Methods

        public override void Refresh()
        {
            Clear();
        }

        #endregion

        #region Subscription Service

        public int Subscribe(string path, PropertyChanged propertyChangedHandler, TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            // Create a subscription channel if not yet done
            if (_subscriptionChannel == null)
            {
                // When we subscribe to a property that's within nested items,
                // we need to navigate back to the root of this client to find
                // the subscription service, and that's where we can subscribe
                WoopsaClientObject rootObject = this;
                while (rootObject.Owner is WoopsaClientObject)
                    rootObject = (WoopsaClientObject)rootObject.Owner;

                if (HasSubscriptionService(rootObject))
                    _subscriptionChannel = new WoopsaClientSubscriptionChannel(rootObject);
                else
                    _subscriptionChannel = new WoopsaClientSubscriptionChannelFallback(rootObject);

                _subscriptionChannel.ValueChange += SubscriptionChannel_ValueChange;
            }

            int subscriptionId = _subscriptionChannel.Register(path, monitorInterval, publishInterval);
            _subscriptionsDictionary.Add(subscriptionId, propertyChangedHandler);

            return subscriptionId;
        }

        public void Unsubscribe(int id)
        {
            if (_subscriptionsDictionary.ContainsKey(id))
                _subscriptionsDictionary.Remove(id);

            if (_subscriptionChannel != null)
                _subscriptionChannel.Unregister(id);
        }

        private void SubscriptionChannel_ValueChange(object sender, WoopsaNotificationsEventArgs notifications)
        {
            foreach (var notification in notifications.Notifications.Notifications)
            {
                if (_subscriptionsDictionary.ContainsKey(notification.SubscriptionId))
                    _subscriptionsDictionary[notification.SubscriptionId](this, notification);
            }
        }

        #endregion

        #region Override PopulateObject

        protected override void PopulateObject()
        {
            base.PopulateObject();

            //TODO : Async !
            _meta = _client.MetaAsync(this.GetPath(_root)).Result;

            foreach (WoopsaPropertyMeta property in _meta.Properties)
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

            foreach (WoopsaMethodMeta method in _meta.Methods)
            {
                var argumentInfos = method.ArgumentInfos.Select(argumentInfo => new WoopsaMethodArgumentInfo(argumentInfo.Name,
                    (WoopsaValueType)Enum.Parse(typeof (WoopsaValueType),
                    argumentInfo.Type))).ToList();

                string methodName = method.Name;

                new WoopsaMethod(this,
                    methodName,
                    (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), method.ReturnType),
                    argumentInfos,
                    args => Invoke(args, argumentInfos, methodName));
            }

            foreach (string item in _meta.Items)
            {
                new WoopsaClientObject(_client, this, item, _root);
            }
        }

        #endregion

        #region Private Methods

        private bool HasSubscriptionService(WoopsaClientObject obj)
        {
            return obj.Items.ByNameOrNull(WoopsaServiceSubscriptionConst.WoopsaServiceSubscriptionName) != null;
        }

        private WoopsaValue GetProperty(IWoopsaProperty property)
        {
            //TODO : Async !
            return _client.ReadAsync(this.GetPath(_root).TrimEnd(WoopsaConst.WoopsaPathSeparator) + WoopsaConst.WoopsaPathSeparator + property.Name).Result;
        }

        private void SetProperty(IWoopsaProperty property, IWoopsaValue value)
        {
            //TODO : Async !
            _client.WriteAsync(this.GetPath(_root).TrimEnd(WoopsaConst.WoopsaPathSeparator) + WoopsaConst.WoopsaPathSeparator + property.Name, value.AsText).Wait();
        }

        private WoopsaValue Invoke(IEnumerable<IWoopsaValue> arguments, List<WoopsaMethodArgumentInfo> argumentInfos, string methodName)
        {
            // This line is needed to avoid multiple enumeration of arguments.
            IWoopsaValue[] woopsaValues = arguments as IWoopsaValue[] ?? arguments.ToArray();

            var namedArguments = new NameValueCollection();

            for (int i = 0; i < argumentInfos.Count; i++)
                namedArguments.Add(argumentInfos[i].Name, woopsaValues[i].AsText);

            //TODO : Async !
            return _client.InvokeAsync(this.GetPath(_root).TrimEnd(WoopsaConst.WoopsaPathSeparator) + WoopsaConst.WoopsaPathSeparator + methodName, namedArguments).Result;
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            if (_subscriptionChannel != null)
                _subscriptionChannel.Dispose();
        }

        #endregion

        #region Private Members

        private readonly WoopsaBaseClient _client;
        private WoopsaMetaResult _meta;
        private readonly Dictionary<int, PropertyChanged> _subscriptionsDictionary = new Dictionary<int, PropertyChanged>();
        private WoopsaClientSubscriptionChannelBase _subscriptionChannel;
        private readonly IWoopsaContainer _root;

        #endregion
    }

    public class WoopsaClientProperty : WoopsaProperty
    {
        #region Constructors

        public WoopsaClientProperty(WoopsaClientObject container, string name, WoopsaValueType type, WoopsaPropertyGet get, WoopsaPropertySet set)
            : base(container, name, type, get, set)
        {
            if (container == null)
                throw new ArgumentNullException("container", string.Format("The argument '{0}' of the constructor cannot be null!", "container"));

            _container = container;
            _subscriptions = new Dictionary<int, EventHandler<WoopsaNotificationEventArgs>>();
        }

        public WoopsaClientProperty(WoopsaClientObject container, string name, WoopsaValueType type, WoopsaPropertyGet get)
            : this(container, name, type, get, null) { }

        #endregion

        #region Public Methods

        public int SubscribeToChanges(EventHandler<WoopsaNotificationEventArgs> callback, TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            int subscriptionId = _container.Subscribe(this.GetPath(), PropertyChangedHandler, monitorInterval, publishInterval);

            lock (_subscriptions)
                _subscriptions.Add(subscriptionId, callback);

            return subscriptionId;
        }

        public void UnsubscribeToChanges(int subscriptionId)
        {
            lock (_subscriptions)
            {
                if (_subscriptions.ContainsKey(subscriptionId))
                {
                    _subscriptions.Remove(subscriptionId);
                    _container.Unsubscribe(subscriptionId);
                }
            }
        }

        #endregion

        #region Public Events

        public event EventHandler<WoopsaNotificationEventArgs> Change
        {
            add
            {
                SubscribeToChanges(value, WoopsaServiceSubscriptionConst.DefaultMonitorInterval, WoopsaServiceSubscriptionConst.DefaultPublishInterval);
            }
            remove
            {
                lock (_subscriptions)
                {
                    var subsPairs = _subscriptions.Where(sub => sub.Value == value).ToArray();
                    foreach (var subscription in subsPairs)
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

        #endregion

        #region Private Handlers

        private void PropertyChangedHandler(object sender, IWoopsaNotification notification)
        {
            lock (_subscriptions)
            {
                if (_subscriptions.ContainsKey(notification.SubscriptionId))
                    _subscriptions[notification.SubscriptionId](this, new WoopsaNotificationEventArgs(notification.Value));
            }
        }

        #endregion

        #region Private Members

        private readonly Dictionary<int, EventHandler<WoopsaNotificationEventArgs>> _subscriptions;
        private readonly WoopsaClientObject _container;

        #endregion
    }

    public class WoopsaNotificationEventArgs : EventArgs
    {
        public WoopsaNotificationEventArgs(IWoopsaValue value)
        {
            Value = value;
        }

        public IWoopsaValue Value { get; private set; }
    }

    public static class WoopsaClientExtensions
    {
        public static WoopsaClientProperty ByName(this IEnumerable<WoopsaClientProperty> properties, string name)
        {
            WoopsaClientProperty result = ByNameOrNull(properties, name);

            if (result == null)
                throw new WoopsaNotFoundException(string.Format("Woopsa property not found : {0}", name));

            return result;
        }

        public static WoopsaClientProperty ByNameOrNull(this IEnumerable<WoopsaClientProperty> properties, string name)
        {
            return properties.FirstOrDefault(item => item.Name == name);
        }
    }
}
