using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    internal class WoopsaClientSubscriptionChannelFallback : WoopsaClientSubscriptionChannelBase
    {
        public WoopsaClientSubscriptionChannelFallback(WoopsaBaseClient client)
        {
            _client = client;
        }

        private WoopsaBaseClient _client;

        public override void Register(string path)
        {
            throw new NotImplementedException();
        }

        public override void Register(string path, int monitorInterval, int publishInterval)
        {
            throw new NotImplementedException();
        }

        public override bool Unregister(string path)
        {
            throw new NotImplementedException();
        }
    }
}
