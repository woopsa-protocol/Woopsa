using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public class WoopsaClient : IDisposable
    {
        public WoopsaClient(string url)
        {
            _client = new WoopsaBaseClient(url);
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
            _clientObject = new WoopsaClientObject(_client, null, meta.Name);
        }

        private WoopsaBaseClient _client;
        private WoopsaClientObject _clientObject;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
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
