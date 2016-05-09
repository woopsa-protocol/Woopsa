using System;

namespace Woopsa
{
    internal abstract class WoopsaClientSubscriptionChannelBase : IDisposable
    {
        #region Abstract Register / Unregister

        public abstract int Register(string path, TimeSpan monitorInterval, TimeSpan publishInterval);

        public abstract bool Unregister(int id);

        #endregion

        #region Public Events

        public event EventHandler<WoopsaNotificationsEventArgs> ValueChange;

        protected virtual void DoValueChanged(WoopsaNotificationsEventArgs e)
        {
            if (ValueChange != null)
                ValueChange(this, e);
        }

        protected virtual void DoValueChanged(IWoopsaNotifications notifications)
        {
            DoValueChanged(new WoopsaNotificationsEventArgs(notifications));
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
