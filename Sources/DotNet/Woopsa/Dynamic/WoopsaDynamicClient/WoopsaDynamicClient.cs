using System;

namespace Woopsa
{
    public class WoopsaDynamicClient : WoopsaDynamicClientObject, IDisposable
    {
        #region Constructors

        public WoopsaDynamicClient(string url, WoopsaConverters customTypeConverters = null) : this(new WoopsaClient(url), customTypeConverters)
        { }

        public WoopsaDynamicClient(WoopsaClient client, WoopsaConverters customTypeConverters = null) : base(customTypeConverters)
        {
            _client = client;
            Refresh();
        }

        #endregion

        #region Properties

        public string Username
        {
            get => _client.Username;
            set => _client.Username = value;
        }

        public string Password
        {
            get => _client.Password;
            set => _client.Password = value;
        }

        #endregion

        #region Fields / Attributes

        private readonly WoopsaClient _client;

        #endregion

        #region Public Methods

        public void Refresh()
        {
            InnerObject = _client.CreateBoundRoot();
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                _client.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
