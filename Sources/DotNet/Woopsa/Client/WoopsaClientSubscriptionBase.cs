using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    internal abstract class WoopsaClientSubscriptionChannelBase
    {
        public virtual event EventHandler<WoopsaNotificationsEventArgs> ValueChange;

        protected virtual void DoValueChanged(IWoopsaNotifications notifications)
        {
            if (ValueChange != null)
            {
                ValueChange(this, new WoopsaNotificationsEventArgs(notifications));
            }
        }

        public abstract int Register(string path, TimeSpan monitorInterval, TimeSpan publishInterval);

        public abstract bool Unregister(int id);
    }
}
