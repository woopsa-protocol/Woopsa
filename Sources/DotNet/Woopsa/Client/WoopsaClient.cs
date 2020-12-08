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
            Uri uri = new Uri(url);
            AuthorityUrl = uri.GetLeftPart(UriPartial.Authority);
            ClientProtocol = new WoopsaClientProtocol(url);
            _container = container;
            WoopsaUnboundClientObject unboundRoot = CreateUnboundRoot("");
            SubscriptionChannel = new WoopsaClientSubscriptionChannel(this,
                unboundRoot, notificationQueueSize);
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

        public WoopsaClientProtocol ClientProtocol { get; }

        public string Url { get; }
        public string AuthorityUrl { get; }

        public string Username
        {
            get => ClientProtocol.Username;
            set => ClientProtocol.Username = value;
        }

        public string Password
        {
            get => ClientProtocol.Password;
            set => ClientProtocol.Password = value;
        }

        public WoopsaClientSubscriptionChannel SubscriptionChannel { get; }

        #endregion

        #region public methods

        public WoopsaBoundClientObject CreateBoundRoot(string name = null)
        {
            WoopsaMetaResult meta = ClientProtocol.Meta(WoopsaConst.WoopsaRootPath);
            return new WoopsaBoundClientObject(this, _container, name ?? meta.Name, null);
        }

        public WoopsaUnboundClientObject CreateUnboundRoot(string name) =>
            new WoopsaUnboundClientObject(this, _container, name, null);

        public void ExecuteMultiRequest(WoopsaClientMultiRequest multiRequest)
        {
            if (multiRequest.Count > 0)
            {
                multiRequest.Reset();
                if (!_disableRemoteMultiRequest)
                    try
                    {
                        WoopsaValue results = _remoteMethodMultiRequest.Invoke(
                            WoopsaValue.WoopsaJsonData(multiRequest.Requests.Serialize()));
                        multiRequest.DispatchResults(results.JsonData);
                    }
                    catch (WoopsaNotFoundException)
                    {
                        _disableRemoteMultiRequest = true;
                    }
                if (_disableRemoteMultiRequest)
                    ExecuteMultiRequestLocally(multiRequest);
            }
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
                    SubscriptionChannel.Dispose();

                if (ClientProtocol != null)
                    ClientProtocol.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private Members

        private void ExecuteMultiRequestLocally(WoopsaClientMultiRequest multiRequest)
        {
            // Execute multi request locally
            foreach (var item in multiRequest.ClientRequests)
            {
                try
                {
                    switch (item.Request.Verb)
                    {
                        case WoopsaFormat.VerbMeta:
                            item.Result = new WoopsaClientRequestResult()
                            {
                                ResultType = WoopsaClientRequestResultType.Meta,
                                Meta = ClientProtocol.Meta(item.Request.Path)
                            };
                            break;
                        case WoopsaFormat.VerbInvoke:
                            item.Result = new WoopsaClientRequestResult()
                            {
                                ResultType = WoopsaClientRequestResultType.Value,
                                Value = ClientProtocol.Invoke(item.Request.Path,                                
                                    item.Request.Arguments.ToNameValueCollection()) 
                            };
                            break;
                        case WoopsaFormat.VerbRead:
                            item.Result = new WoopsaClientRequestResult()
                            {
                                ResultType = WoopsaClientRequestResultType.Value,
                                Value = ClientProtocol.Read(item.Request.Path)
                            };
                            break;
                        case WoopsaFormat.VerbWrite:
                            ClientProtocol.Write(item.Request.Path, item.Request.Value);
                            item.Result = new WoopsaClientRequestResult()
                            {
                                ResultType = WoopsaClientRequestResultType.Value,
                                Value = WoopsaValue.Null
                            };
                            break;
                    }
                }
                catch (Exception e)
                {
                    item.Result = new WoopsaClientRequestResult()
                    {
                        ResultType = WoopsaClientRequestResultType.Error,
                        Error = e
                    };
                    item.IsDone = true;
                }
            }
        }

        private readonly WoopsaContainer _container;
        private WoopsaMethod _remoteMethodMultiRequest;
        private bool _disableRemoteMultiRequest;

        #endregion
    }
}
