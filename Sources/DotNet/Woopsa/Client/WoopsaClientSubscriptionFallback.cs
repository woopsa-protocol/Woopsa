using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    internal class WoopsaClientSubscriptionChannelFallback : WoopsaClientSubscriptionChannelBase
    {
        #region Constructors

        public WoopsaClientSubscriptionChannelFallback(WoopsaClientObject client)
        {
            _client = client;
            _service = new SubscriptionService(_client);
            _channel = new WoopsaClientSubscriptionChannel(_client);
        }

        #endregion

        #region Override Event ValueChange

        // TODO CJI From CJI : Why do we need to override the event here?
        public override event EventHandler<WoopsaNotificationsEventArgs> ValueChange
        {
            add { _channel.ValueChange += value; }
            remove { _channel.ValueChange -= value; }
        }

        #endregion

        #region Override Register / Unregister

        public override int Register(string path, TimeSpan monitorInterval, TimeSpan publishInterval)
        {
            return _channel.Register(path, monitorInterval, publishInterval);
        }

        public override bool Unregister(int id)
        {
            return _channel.Unregister(id);
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            _client.Dispose();
            _service.Dispose();
        }

        #endregion

        #region Private Members

        private readonly WoopsaClientObject _client;
        private readonly SubscriptionService _service;
        private readonly WoopsaClientSubscriptionChannel _channel;

        #endregion
    }
}
