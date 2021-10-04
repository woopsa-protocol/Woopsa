using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

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

        #region Properties

        public WoopsaClient Client { get; }

        public IWoopsaContainer Root { get; }

        #endregion

        #region Public Methods

        public WoopsaClientSubscription Subscribe(string relativePath,
            EventHandler<WoopsaNotificationEventArgs> propertyChangedHandler,
            TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            return Client.SubscriptionChannel.Subscribe(
                        WoopsaUtils.CombinePath(this.GetPath(), relativePath),
                        relativePath,
                        (sender, e) => propertyChangedHandler?.Invoke(this, e),
                        monitorInterval,
                        publishInterval
                        );
        }

        public WoopsaClientSubscription Subscribe(string relativePath,
            EventHandler<WoopsaNotificationEventArgs> propertyChangedHandler)
        {
            return Subscribe(relativePath, propertyChangedHandler,
                WoopsaSubscriptionServiceConst.DefaultMonitorInterval,
                WoopsaSubscriptionServiceConst.DefaultPublishInterval);
        }

        public WoopsaClientSubscription SubscribeAsync(string relativePath,
            EventHandler<WoopsaNotificationEventArgs> propertyChangedHandler,
            TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            return Client.SubscriptionChannel.Subscribe(
                        WoopsaUtils.CombinePath(this.GetPath(), relativePath),
                        relativePath,
                        (sender, e) => propertyChangedHandler?.Invoke(this, e),
                        monitorInterval,
                        publishInterval
                        );
        }

        public WoopsaClientSubscription SubscribeAsync(string relativePath,
            EventHandler<WoopsaNotificationEventArgs> propertyChangedHandler)
        {
            return SubscribeAsync(relativePath, propertyChangedHandler,
                WoopsaSubscriptionServiceConst.DefaultMonitorInterval,
                WoopsaSubscriptionServiceConst.DefaultPublishInterval);
        }

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
                        args => Invoke(args, argumentInfos, name),
                        async args => await InvokeAsync(args, argumentInfos, name));
        }

        //protected WoopsaMethod CreateMethodAsync(string name, WoopsaMethodArgumentInfo[] argumentInfos,
        //    WoopsaValueType returnType)
        //{
        //    return new WoopsaMethod(this,
        //                name,
        //                returnType,
        //                argumentInfos,
        //                args => Invoke(args, argumentInfos, name),
        //                async args => await InvokeAsync(args, argumentInfos, name));
        //}

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

        private async Task<WoopsaValue> InvokeAsync(IEnumerable<IWoopsaValue> arguments, WoopsaMethodArgumentInfo[] argumentInfos, string methodName)
        {
            // This line is needed to avoid multiple enumeration of arguments.
            IWoopsaValue[] woopsaValues = arguments as IWoopsaValue[] ?? arguments.ToArray();

            var namedArguments = new NameValueCollection();

            for (int i = 0; i < argumentInfos.Length; i++)
                namedArguments.Add(argumentInfos[i].Name, woopsaValues[i].AsText);

            return await Client.ClientProtocol.InvokeAsync(this.GetPath(Root).TrimEnd(WoopsaConst.WoopsaPathSeparator) + WoopsaConst.WoopsaPathSeparator + methodName, namedArguments);
        }

        #endregion
    }
}