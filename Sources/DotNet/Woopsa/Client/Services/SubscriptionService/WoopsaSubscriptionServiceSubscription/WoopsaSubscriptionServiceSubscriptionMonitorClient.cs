using System;

namespace Woopsa
{
    public class WoopsaSubscriptionServiceSubscriptionMonitorClient :
        WoopsaSubscriptionServiceSubscriptionMonitor
    {
        #region Constructor

        public WoopsaSubscriptionServiceSubscriptionMonitorClient(
        WoopsaSubscriptionChannel channel,
        WoopsaBaseClientObject root,
        int subscriptionId, string propertyPath,
        TimeSpan monitorInterval, TimeSpan publishInterval) :
            base(channel, root, subscriptionId, propertyPath, monitorInterval, publishInterval)
        {
        }

        #endregion

        #region Protected Methods

        protected override bool GetWatchedPropertyValue(out IWoopsaValue value)
        {
            try
            {
                value = ((WoopsaBaseClientObject)Root).Client.ClientProtocol.Read(PropertyPath);
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }

        #endregion
    }
}