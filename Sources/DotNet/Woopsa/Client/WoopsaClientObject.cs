using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Woopsa
{
    public class WoopsaBaseClientObject : WoopsaObject
    {
        #region Constructors

        internal WoopsaBaseClientObject(WoopsaClient client, WoopsaContainer container, string name, IWoopsaContainer root)
            : base(container, name)
        {
            Client = client;
            Root = root ?? this;
        }

        #endregion

        public WoopsaClient Client { get; private set; }
        public IWoopsaContainer Root { get; private set; }

        public WoopsaClientSubscription Subscribe(string relativePath,
            EventHandler<WoopsaNotificationEventArgs> propertyChangedHandler,
            TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            return Client.SubscriptionChannel.Subscribe(
                        WoopsaExtensions.CombinePath(this.GetPath(), relativePath),
                        relativePath,
                        monitorInterval,
                        publishInterval,
                        (sender, e) =>
                            {
                                if (propertyChangedHandler != null)
                                    propertyChangedHandler(this, e);
                            }
                        );
        }
        
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
            return Client.ClientProtocol.Read(this.GetPath(Root).TrimEnd(WoopsaConst.WoopsaPathSeparator) + WoopsaConst.WoopsaPathSeparator + property.Name);
        }

        private void SetProperty(IWoopsaProperty property, IWoopsaValue value)
        {
            Client.ClientProtocol.Write(this.GetPath(Root).TrimEnd(WoopsaConst.WoopsaPathSeparator) + WoopsaConst.WoopsaPathSeparator + property.Name, value.AsText);
        }

        private WoopsaValue Invoke(IEnumerable<IWoopsaValue> arguments, WoopsaMethodArgumentInfo[] argumentInfos, string methodName)
        {
            // This line is needed to avoid multiple enumeration of arguments.
            IWoopsaValue[] woopsaValues = arguments as IWoopsaValue[] ?? arguments.ToArray();

            var namedArguments = new NameValueCollection();

            for (int i = 0; i < argumentInfos.Length; i++)
                namedArguments.Add(argumentInfos[i].Name, woopsaValues[i].AsText);

            return Client.ClientProtocol.Invoke(this.GetPath(Root).TrimEnd(WoopsaConst.WoopsaPathSeparator) + WoopsaConst.WoopsaPathSeparator + methodName, namedArguments);
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
                throw new ArgumentNullException("container", string.Format("The argument '{0}' of the WoopsaClientProperty constructor cannot be null!", "container"));
        }

        public WoopsaClientProperty(WoopsaBaseClientObject container, string name, WoopsaValueType type, WoopsaPropertyGet get)
            : this(container, name, type, get, null) { }

        #endregion

        public WoopsaClientSubscription Subscribe(EventHandler<WoopsaNotificationEventArgs> handler, TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            return ((WoopsaBaseClientObject)Owner).Subscribe(Name,
                (sender, e) =>
                {
                    if (handler != null)
                        handler(this, e);
                }, 
                monitorInterval, publishInterval);
        }
        public WoopsaClientSubscription Subscribe(EventHandler<WoopsaNotificationEventArgs> handler)
        {
            return Subscribe(handler,
                WoopsaServiceSubscriptionConst.DefaultMonitorInterval,
                WoopsaServiceSubscriptionConst.DefaultPublishInterval);
        }

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

        internal WoopsaBoundClientObject(WoopsaClient client, WoopsaContainer container, string name, IWoopsaContainer root)
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

            meta = Client.ClientProtocol.Meta(this.GetPath(Root));
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

        internal WoopsaUnboundClientObject(WoopsaClient client, WoopsaContainer container, string name, IWoopsaContainer root)
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