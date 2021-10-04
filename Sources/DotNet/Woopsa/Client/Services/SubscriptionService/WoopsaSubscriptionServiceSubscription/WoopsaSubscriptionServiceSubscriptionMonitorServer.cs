using System;

namespace Woopsa
{
    public class WoopsaSubscriptionServiceSubscriptionMonitorServer :
        WoopsaSubscriptionServiceSubscriptionMonitor
    {
        #region Constructors

        public WoopsaSubscriptionServiceSubscriptionMonitorServer(
        WoopsaSubscriptionChannel channel,
        WoopsaContainer root,
        int subscriptionId, string propertyPath,
        TimeSpan monitorInterval, TimeSpan publishInterval) :
            base(channel, root, subscriptionId, propertyPath, monitorInterval, publishInterval)
        {
        }

        #endregion

        #region Fields / Attributes

        private IWoopsaProperty _watchedProperty;

        #endregion

        #region Protected Methods

        protected override bool GetWatchedPropertyValue(out IWoopsaValue value)
        {
            try
            {
                IWoopsaProperty currentlyWatchedProperty;
                IWoopsaValue currentlyWatchedPropertyValue;

                if ((_watchedProperty == null) ||
                    (_watchedProperty is WoopsaElement && ((WoopsaElement)_watchedProperty).IsDisposed))
                {
                    IWoopsaProperty newWatchedProperty;

                    newWatchedProperty = Root.ByPathOrNull(PropertyPath) as IWoopsaProperty;
                    if (newWatchedProperty != null)
                    {
                        currentlyWatchedProperty = newWatchedProperty;
                        currentlyWatchedPropertyValue = currentlyWatchedProperty.Value;
                    }
                    else
                    {
                        currentlyWatchedProperty = _watchedProperty;
                        currentlyWatchedPropertyValue = WoopsaValue.Null;
                    }
                }
                else
                {
                    currentlyWatchedProperty = _watchedProperty;
                    currentlyWatchedPropertyValue = _watchedProperty.Value;
                }
                if (currentlyWatchedProperty != null)
                {
                    if (OnCanWatch(this, currentlyWatchedProperty))
                    {
                        value = currentlyWatchedPropertyValue;
                        _watchedProperty = currentlyWatchedProperty;
                        return true;
                    }
                    else
                    {
                        value = WoopsaValue.Null;
                        return false;
                    }
                }
                else
                {
                    value = WoopsaValue.Null;
                    return true;
                }
            }
            catch (Exception)
            {
                // The property might have become invalid, search it new the next time
                _watchedProperty = null;
                throw;
            }
        }

        #endregion

        #region Private Methods

        private bool OnCanWatch(
            BaseWoopsaSubscriptionServiceSubscription subscription,
            IWoopsaProperty itemProperty)
        {
            return Channel.OnCanWatch(subscription, itemProperty);
        }

        #endregion

        #region IDisposable

        // ! Do not dispose the _watchedProperty as we are not the owner of this object

        #endregion
    }
}