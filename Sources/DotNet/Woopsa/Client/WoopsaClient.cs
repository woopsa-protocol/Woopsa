using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public class WoopsaClient : IDisposable
    {
        public WoopsaClient(string url) : this(url, null, null) { }

        public WoopsaClient(string url, WoopsaContainer container, string name)
        {
            _client = new WoopsaBaseClient(url);
            _container = container;
            _name = name;
            if (_container != null)
                Refresh();
        }

        public string Username
        {
            get
            {
                return _client.Username;
            }
            set
            {
                _client.Username = value;
            }
        }

        public string Password
        {
            get
            {
                return _client.Password;
            }
            set
            {
                _client.Password = value;
            }
        }

        public WoopsaClientObject Root 
        { 
            get
            {
                if (_clientObject == null)
                    Refresh();
                return _clientObject; 
            } 
        }

        public void Refresh()
        {
            WoopsaMetaResult meta = _client.Meta(WoopsaConst.WoopsaRootPath);
            if (_name == null)
                _name = meta.Name;
            _clientObject = new WoopsaClientObject(_client, _container, _name, null);
        }

        private WoopsaBaseClient _client;
        private WoopsaClientObject _clientObject;
        private WoopsaContainer _container;
        private string _name;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if ( _clientObject != null )
                    _clientObject.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
