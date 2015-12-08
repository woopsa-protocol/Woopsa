using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    internal class WoopsaClientSubscriptionChannelFallback : WoopsaClientSubscriptionChannelBase
    {
        public WoopsaClientSubscriptionChannelFallback(WoopsaClientObject client)
        {
            _client = client;
            _service = new SubscriptionService(_client);
            _channel = new WoopsaClientSubscriptionChannel(_client);
        }

        public override event EventHandler<WoopsaNotificationsEventArgs> ValueChange
        {
            add
            {
                _channel.ValueChange += value;
            }
            remove
            {
                _channel.ValueChange -= value;
            }
        }

        private WoopsaClientObject _client;
        
        public override int Register(string path, TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            return _channel.Register(path, monitorInterval, publishInterval);
        }

        public override bool Unregister(int id)
        {
            return _channel.Unregister(id);
        }

        private SubscriptionService _service;
        private WoopsaClientSubscriptionChannel _channel;
    }
}
