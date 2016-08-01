using System;

namespace Woopsa
{
    public class WoopsaSubscriptionService : WoopsaObject
    {
        #region Constructors

        public WoopsaSubscriptionService(WoopsaServer server, WoopsaContainer container)
            : base(container, WoopsaSubscriptionServiceConst.WoopsaServiceSubscriptionName)
        {
            _server = server;
            _subscriptionServiceImplementation = new WoopsaSubscriptionServiceImplementation(container, true);
            _subscriptionServiceImplementation.BeforeWoopsaModelAccess += 
                (sender, e) => { server.OnBeforeWoopsaModelAccess(); };
            _subscriptionServiceImplementation.AfterWoopsaModelAccess +=
                (sender, e) => { server.OnAfterWoopsaModelAccess(); };
            MethodCreateSubscriptionChannel = new WoopsaMethod(
                this,
                WoopsaSubscriptionServiceConst.WoopsaCreateSubscriptionChannel,
                WoopsaValueType.Integer,
                new WoopsaMethodArgumentInfo[] { new WoopsaMethodArgumentInfo(WoopsaSubscriptionServiceConst.WoopsaNotificationQueueSize, WoopsaValueType.Integer) },
                arguments => _subscriptionServiceImplementation.CreateSubscriptionChannel(arguments[0].ToInt32())
            );

            MethodRegisterSubscription = new WoopsaMethod(
                this,
                WoopsaSubscriptionServiceConst.WoopsaRegisterSubscription,
                WoopsaValueType.Integer,
                new WoopsaMethodArgumentInfo[] {
                    new WoopsaMethodArgumentInfo(WoopsaSubscriptionServiceConst.WoopsaSubscriptionChannel, WoopsaValueType.Integer),
                    new WoopsaMethodArgumentInfo(WoopsaSubscriptionServiceConst.WoopsaPropertyLink, WoopsaValueType.WoopsaLink),
                    new WoopsaMethodArgumentInfo(WoopsaSubscriptionServiceConst.WoopsaMonitorInterval, WoopsaValueType.TimeSpan),
                    new WoopsaMethodArgumentInfo(WoopsaSubscriptionServiceConst.WoopsaPublishInterval, WoopsaValueType.TimeSpan)
                },
                arguments =>
                {
                    return _subscriptionServiceImplementation.RegisterSubscription(
                        arguments[0].ToInt32(), arguments[1].DecodeWoopsaLocalLink(),
                        arguments[2].ToTimeSpan(), arguments[3].ToTimeSpan());
                });

            MethodUnregisterSubscription = new WoopsaMethod(
                this,
                WoopsaSubscriptionServiceConst.WoopsaUnregisterSubscription,
                WoopsaValueType.Logical,
                new WoopsaMethodArgumentInfo[] {
                    new WoopsaMethodArgumentInfo(WoopsaSubscriptionServiceConst.WoopsaSubscriptionChannel, WoopsaValueType.Integer),
                    new WoopsaMethodArgumentInfo(WoopsaSubscriptionServiceConst.WoopsaSubscriptionId, WoopsaValueType.Integer)
                },
                arguments =>
                {
                    return _subscriptionServiceImplementation.UnregisterSubscription(
                        arguments[0].ToInt32(), arguments[1].ToInt32());
                });

            MethodWaitNotification = new WoopsaMethod(
                this,
                WoopsaSubscriptionServiceConst.WoopsaWaitNotification,
                WoopsaValueType.JsonData,
                new WoopsaMethodArgumentInfo[] {
                    new WoopsaMethodArgumentInfo(WoopsaSubscriptionServiceConst.WoopsaSubscriptionChannel, WoopsaValueType.Integer),
                    new WoopsaMethodArgumentInfo(WoopsaSubscriptionServiceConst.WoopsaLastNotificationId, WoopsaValueType.Integer)
                },
                arguments =>
                {
                    using (var accessFreeSection = _server.EnterModelAccessFreeSection())
                        return new WoopsaValue(_subscriptionServiceImplementation.WaitNotification(
                            arguments[0].ToInt32(), arguments[1].ToInt32()));
                });
        }

        public override void Refresh()
        {
            base.Refresh();
            _subscriptionServiceImplementation.Refresh();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (_subscriptionServiceImplementation != null)
                {
                    _subscriptionServiceImplementation.Dispose();
                    _subscriptionServiceImplementation = null;
                }
            }
        }

        #endregion

        public WoopsaMethod MethodCreateSubscriptionChannel { get; private set; }
        public WoopsaMethod MethodRegisterSubscription { get; private set; }
        public WoopsaMethod MethodUnregisterSubscription { get; private set; }
        public WoopsaMethod MethodWaitNotification { get; private set; }

        #region Private Members

        WoopsaServer _server;
        WoopsaSubscriptionServiceImplementation _subscriptionServiceImplementation;

        #endregion
    }
}
