using System;

namespace Woopsa
{
    public class WoopsaClient : IDisposable
    {
        #region Constructors

        public WoopsaClient(string url) : this(url, null) { }

        public WoopsaClient(string url, WoopsaContainer container)
        {
            _client = new WoopsaClientProtocol(url);
            _container = container;
        }

        #endregion

        #region Public Properties

        public string Username
        {
            get { return _client.Username; }
            set { _client.Username = value; }
        }

        public string Password
        {
            get { return _client.Password; }
            set { _client.Password = value; }
        }

        public WoopsaBoundClientObject CreateBoundRoot(string name = null)
        {
            WoopsaMetaResult meta = _client.Meta(WoopsaConst.WoopsaRootPath);
            return new WoopsaBoundClientObject(_client, _container, name ?? meta.Name, null);
        }

        public WoopsaUnboundClientObject CreateUnboundRoot(string name)
        {
            return new WoopsaUnboundClientObject(_client, _container, name, null);
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TODO : a deplacer
/*                _clientObject.Terminate();
                _client.Terminate();
                if (_clientObject != null)
                {
                    _clientObject.Dispose();
                    _clientObject = null;
                }*/
                if (_client != null)
                {
                    _client.Dispose();
                    _client = null;
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

        private WoopsaClientProtocol _client;
        private readonly WoopsaContainer _container;

        #endregion
    }
}
