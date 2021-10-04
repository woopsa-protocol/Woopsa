using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public static class WoopsaServerUtils
    {
        public static bool IsContextWoopsaThread
        {
            get
            {
                return
                    IsContextWebServer ||
                    IsContextWoopsaClientSubscriptionThread ||
                    IsContextWoopsaSubscriptionServiceImplementation;
            }
        }

        internal static bool IsContextWebServer => WebServer.Current != null;

        internal static bool IsContextWoopsaClientSubscriptionThread => WoopsaClientSubscriptionChannel.CurrentChannel != null;

        internal static bool IsContextWoopsaSubscriptionServiceImplementation => WoopsaSubscriptionServiceImplementation.CurrentService != null;

        public static bool IsContextWoopsaTerminatingThread
        {
            get
            {
                if (WebServer.Current != null)
                    return WebServer.Current.Disposed;
                else if (WoopsaClientSubscriptionChannel.CurrentChannel != null)
                    return WoopsaClientSubscriptionChannel.CurrentChannel.Terminated;
                else if (WoopsaSubscriptionServiceImplementation.CurrentService != null)
                    return WoopsaSubscriptionServiceImplementation.CurrentService.Terminated;
                else
                    return false;
            }
        }
    }
}
