using System;

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
            _channel.ValueChange += _channel_ValueChange;
        }

        private void _channel_ValueChange(object sender, WoopsaNotificationsEventArgs e)
        {
            DoValueChanged(e);
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
            base.Dispose(disposing);
            if (disposing)
            {
                _channel.Dispose();
                _service.Dispose();
            }
        }

        #endregion

        #region Private Members

        private readonly WoopsaClientObject _client;
        private readonly SubscriptionService _service;
        private readonly WoopsaClientSubscriptionChannel _channel;

        #endregion
    }
}
