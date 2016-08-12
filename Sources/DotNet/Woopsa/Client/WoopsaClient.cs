using System;

namespace Woopsa
{
    public class WoopsaClient : IDisposable
    {
        #region Constructors

        const int DefaultNotificationQueueSize = 1000;

        public WoopsaClient(string url) : this(url, null) { }

        public WoopsaClient(string url, WoopsaContainer container,
            int notificationQueueSize = DefaultNotificationQueueSize)
        {
            ClientProtocol = new WoopsaClientProtocol(url);
            _container = container;
            WoopsaUnboundClientObject unboundRoot = CreateUnboundRoot("");
            SubscriptionChannel = new WoopsaClientSubscriptionChannel(unboundRoot, notificationQueueSize);
            _remoteMethodMultiRequest = unboundRoot.GetMethod(
                WoopsaMultiRequestConst.WoopsaMultiRequestMethodName,
                WoopsaValueType.JsonData,
                new WoopsaMethodArgumentInfo[] 
                {
                    new WoopsaMethodArgumentInfo(WoopsaMultiRequestConst.WoopsaMultiRequestArgumentName, WoopsaValueType.JsonData)
                });
        }

        #endregion

        #region Public Properties

        public WoopsaClientProtocol ClientProtocol { get; private set; }

        public string Username
        {
            get { return ClientProtocol.Username; }
            set { ClientProtocol.Username = value; }
        }

        public string Password
        {
            get { return ClientProtocol.Password; }
            set { ClientProtocol.Password = value; }
        }

        public WoopsaClientSubscriptionChannel SubscriptionChannel { get; private set; }

        #endregion

        #region public methods

        public WoopsaBoundClientObject CreateBoundRoot(string name = null)
        {
            WoopsaMetaResult meta = ClientProtocol.Meta(WoopsaConst.WoopsaRootPath);
            return new WoopsaBoundClientObject(this, _container, name ?? meta.Name, null);
        }

        public WoopsaUnboundClientObject CreateUnboundRoot(string name)
        {
            return new WoopsaUnboundClientObject(this, _container, name, null);
        }

        public void ExecuteMultiRequest(WoopsaClientMultiRequest multiRequest)
        {            
            multiRequest.Reset();
            WoopsaValue results = _remoteMethodMultiRequest.Invoke(
                WoopsaValue.WoopsaJsonData(multiRequest.Requests.Serialize()));
            multiRequest.DispatchResults(results.JsonData);
        }

        #endregion


        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                SubscriptionChannel.Terminate();
                ClientProtocol.Terminate();
                if (SubscriptionChannel != null)
                {
                    SubscriptionChannel.Dispose();
                    SubscriptionChannel = null;
                }
                if (ClientProtocol != null)
                {
                    ClientProtocol.Dispose();
                    ClientProtocol = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private Members

        private readonly WoopsaContainer _container;
        private WoopsaMethod _remoteMethodMultiRequest;

        #endregion
    }
}
