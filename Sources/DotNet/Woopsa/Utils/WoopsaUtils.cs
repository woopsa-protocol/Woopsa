using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Woopsa
{
    static public class WoopsaUtils
    {
        public static bool IsContextWoopsaThread
        {
            get
            {
                return 
                    IsContextWebServerThread ||
                    IsContextWoopsaClientSubscriptionThread ||
                    IsContextWoopsaSubscriptionServiceImplementation;
            }
        }
        internal static bool IsContextWebServerThread
        {
            get { return WebServer.CurrentWebServer != null; }
        }
        internal static bool IsContextWoopsaClientSubscriptionThread
        {
            get { return WoopsaClientSubscriptionChannel.CurrentChannel != null; }
        }

        internal static bool IsContextWoopsaSubscriptionServiceImplementation
        {
            get { return WoopsaSubscriptionServiceImplementation.CurrentService != null; }
        }

        public static bool IsContextWoopsaTerminatingThread
        {
            get
            {
                if (WebServer.CurrentWebServer != null)
                    return WebServer.CurrentWebServer.Aborted;
                else if (WoopsaClientSubscriptionChannel.CurrentChannel != null)
                    return WoopsaClientSubscriptionChannel.CurrentChannel.Terminated;
                else if (WoopsaSubscriptionServiceImplementation.CurrentService != null)
                    return WoopsaSubscriptionServiceImplementation.CurrentService.Terminated;
                else
                    return false;
            }
        }

        public static string CombinePath(string basePath, string relativePath)
        {
            return basePath.TrimEnd(WoopsaConst.WoopsaPathSeparator) +
                WoopsaConst.WoopsaPathSeparator +
                relativePath.TrimStart(WoopsaConst.WoopsaPathSeparator);
        }

    }
}
