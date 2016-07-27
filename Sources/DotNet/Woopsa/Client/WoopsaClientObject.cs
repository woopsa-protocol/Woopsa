using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Woopsa
{
    public class WoopsaBaseClientObject : WoopsaObject
    {
        #region Constructors

        internal WoopsaBaseClientObject(WoopsaClientProtocol client, WoopsaContainer container, string name, IWoopsaContainer root)
            : base(container, name)
        {
            Client = client;
            Root = root ?? this;
        }

        #endregion

        public WoopsaClientProtocol Client { get; private set; }
        public IWoopsaContainer Root { get; private set; }

        #region Public Delegates

        public delegate void PropertyChanged(object sender, IWoopsaNotification notification);

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
                WoopsaBaseClientObject rootObject = this;
                while (rootObject.Owner is WoopsaBoundClientObject)
                    rootObject = (WoopsaBoundClientObject)rootObject.Owner;

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

        public void Terminate()
        {
            // TODO : à déplacer
            if (_subscriptionChannel != null)
                _subscriptionChannel.Terminate();
        }

        private void SubscriptionChannel_ValueChange(object sender, WoopsaNotificationsEventArgs notifications)
        {
            foreach (var notification in notifications.Notifications.Notifications)
            {
                if (_subscriptionsDictionary.ContainsKey(notification.SubscriptionId))
                    _subscriptionsDictionary[notification.SubscriptionId](this, notification);
            }
        }

        private bool HasSubscriptionService(WoopsaBaseClientObject obj)
        {
            return obj.Items.ByNameOrNull(WoopsaServiceSubscriptionConst.WoopsaServiceSubscriptionName) != null;
        }

        private readonly Dictionary<int, PropertyChanged> _subscriptionsDictionary = new Dictionary<int, PropertyChanged>();
        private WoopsaClientSubscriptionChannelBase _subscriptionChannel;

        #endregion

        #region protected 

        protected WoopsaClientProperty CreateProperty(string name, WoopsaValueType type, bool readOnly)
        {
            if (readOnly)
                return new WoopsaClientProperty(this, name, type, GetProperty);
            else
                return new WoopsaClientProperty(this, name, type, GetProperty, SetProperty);
        }

        protected WoopsaMethod CreateMethod(string name, WoopsaMethodArgumentInfo[] argumentInfos, 
            WoopsaValueType returnType)
        {
            return new WoopsaMethod(this,
                        name,
                        returnType,
                        argumentInfos,
                        args => Invoke(args, argumentInfos, name));
        }

        #endregion

        #region Private Methods
        private WoopsaValue GetProperty(IWoopsaProperty property)
        {
            return Client.Read(this.GetPath(Root).TrimEnd(WoopsaConst.WoopsaPathSeparator) + WoopsaConst.WoopsaPathSeparator + property.Name);
        }

        private void SetProperty(IWoopsaProperty property, IWoopsaValue value)
        {
            Client.Write(this.GetPath(Root).TrimEnd(WoopsaConst.WoopsaPathSeparator) + WoopsaConst.WoopsaPathSeparator + property.Name, value.AsText);
        }

        private WoopsaValue Invoke(IEnumerable<IWoopsaValue> arguments, WoopsaMethodArgumentInfo[] argumentInfos, string methodName)
        {
            // This line is needed to avoid multiple enumeration of arguments.
            IWoopsaValue[] woopsaValues = arguments as IWoopsaValue[] ?? arguments.ToArray();

            var namedArguments = new NameValueCollection();

            for (int i = 0; i < argumentInfos.Length; i++)
                namedArguments.Add(argumentInfos[i].Name, woopsaValues[i].AsText);

            return Client.Invoke(this.GetPath(Root).TrimEnd(WoopsaConst.WoopsaPathSeparator) + WoopsaConst.WoopsaPathSeparator + methodName, namedArguments);
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (_subscriptionChannel != null)
                {
                    _subscriptionChannel.Dispose();
                    _subscriptionChannel = null;
                }
            }
        }

        #endregion

    }

    public class WoopsaClientProperty : WoopsaProperty
    {
        #region Constructors

        public WoopsaClientProperty(WoopsaBaseClientObject container, string name, WoopsaValueType type, WoopsaPropertyGet get, WoopsaPropertySet set)
            : base(container, name, type, get, set)
        {
            if (container == null)
                throw new ArgumentNullException("container", string.Format("The argument '{0}' of the constructor cannot be null!", "container"));

            _container = container;
            _subscriptions = new Dictionary<int, EventHandler<WoopsaNotificationEventArgs>>();
        }

        public WoopsaClientProperty(WoopsaBaseClientObject container, string name, WoopsaValueType type, WoopsaPropertyGet get)
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
        private readonly WoopsaBaseClientObject _container;

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

    public class WoopsaBoundClientObject : WoopsaBaseClientObject
    {
        #region Constructors

        internal WoopsaBoundClientObject(WoopsaClientProtocol client, WoopsaContainer container, string name, IWoopsaContainer root)
            : base(client, container, name, root)
        {
        }

        #endregion

        #region Public Methods

        public override void Refresh()
        {
            Clear();
        }

        #endregion

        #region Override PopulateObject

        protected override void PopulateObject()
        {
            WoopsaMetaResult meta;
            base.PopulateObject();

            meta = Client.Meta(this.GetPath(Root));
            // Create properties
            if (meta.Properties != null)
                foreach (WoopsaPropertyMeta property in meta.Properties)
                    CreateProperty(
                        property.Name,
                            (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), property.Type),
                            property.ReadOnly);
            // Create methods
            if (meta.Methods != null)
                foreach (WoopsaMethodMeta method in meta.Methods)
                {
                    var argumentInfos = method.ArgumentInfos.Select(argumentInfo => new WoopsaMethodArgumentInfo(argumentInfo.Name,
                        (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType),
                        argumentInfo.Type))).ToArray();
                    CreateMethod(method.Name, argumentInfos, (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), method.ReturnType));
                }
            // Create items
            if (meta.Items != null)
                foreach (string item in meta.Items)
                {
                    new WoopsaBoundClientObject(Client, this, item, Root);
                }
        }

        #endregion

    }

    public class WoopsaUnboundClientObject : WoopsaBaseClientObject
    {
        #region Constructors

        internal WoopsaUnboundClientObject(WoopsaClientProtocol client, WoopsaContainer container, string name, IWoopsaContainer root)
            : base(client, container, name, root)
        {
        }

        #endregion

        #region public methods

        public WoopsaClientProperty GetProperty(string name, WoopsaValueType type, bool readOnly)
        {
            WoopsaProperty result = Properties.ByNameOrNull(name);
            if (result != null)
            {
                if (result.Type != type)
                    throw new Exception(string.Format(
                        "A property with then name {0} exists, but with the type {1} instead of {2}",
                        name, result.Type, type));
                else if (result.IsReadOnly != readOnly)
                    throw new Exception(string.Format(
                        "A property with then name {0} exists, but with the readonly flag {1} instead of {2}",
                        name, result.IsReadOnly, readOnly));
                else if (!(result is WoopsaClientProperty))
                    throw new Exception(string.Format(
                        "A property with then name {0} exists, but it is not of the type WoopsaClientProperty", name));
                else
                    return result as WoopsaClientProperty;
            }
            else
                return base.CreateProperty(name, type, readOnly);
        }

        public WoopsaMethod GetMethod(string name, WoopsaMethodArgumentInfo[] argumentInfos,
            WoopsaValueType returnType)
        {
            WoopsaMethod result = Methods.ByNameOrNull(name);
            if (result != null)
            {
                if (result.ReturnType != returnType)
                    throw new Exception(string.Format(
                        "A method with then name {0} exists, but with the return type {1} instead of {2}",
                        name, result.ReturnType, returnType));
                else if (result.ArgumentInfos.IsSame(argumentInfos))
                    throw new Exception(string.Format(
                        "A method with then name {0} exists, but with different arguments",
                        name));
                else
                    return result;
            }
            else
                return base.CreateMethod(name, argumentInfos, returnType);
        }

        public WoopsaUnboundClientObject GetUnboundItem(string name)
        {
            return new WoopsaUnboundClientObject(Client, this, name, Root);
        }

        public WoopsaBoundClientObject GetBoundItem(string name)
        {
            return new WoopsaBoundClientObject(Client, this, name, Root);
        }

        #endregion

    }
}