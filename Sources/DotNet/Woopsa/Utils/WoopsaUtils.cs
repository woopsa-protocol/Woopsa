using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
                RemoveInitialSeparator(relativePath);
        }

        public static string RemoveInitialSeparator(string path)
        {
            if (path.Length >= 1)
                if (path[0] == WoopsaConst.WoopsaPathSeparator)
                    return path.Substring(1);
                else
                    return path;
            else
                return path;
        }

        public static NameValueCollection ToNameValueCollection(
            this Dictionary<string, string> dictionary)
        {
            NameValueCollection result = new  NameValueCollection();
            foreach (var item in dictionary)
                result.Add(item.Key, item.Value);
            return result;
        }

        public static TimeSpan Multiply(this TimeSpan timeSpan, int n)
        {
            return TimeSpan.FromTicks(timeSpan.Ticks * n);
        }
    }
}
