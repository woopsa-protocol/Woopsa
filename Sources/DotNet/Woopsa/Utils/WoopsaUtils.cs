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
            this Dictionary<string, WoopsaValue> dictionary)
        {
            NameValueCollection result = new NameValueCollection();
            foreach (var item in dictionary)
                result.Add(item.Key, item.Value.AsText);
            return result;
        }

        public static TimeSpan Multiply(this TimeSpan timeSpan, int n)
        {
            return TimeSpan.FromTicks(timeSpan.Ticks * n);
        }

        /// <summary>
        /// Used to generate incremental numbers identifiers, user for example to uniquely
        /// identify in an ordered way the subscriptions.
        /// </summary>
        /// <returns></returns>
        public static UInt64 NextIncrementalObjectId()
        {
            lock (_instanceIndexLock)
            {
                _instanceIndex++;
                return _instanceIndex;
            }
        }

        private static UInt64 _instanceIndex;
        private static object _instanceIndexLock = new object();


        #region Exceptions utilities
        public static Exception RootException(this Exception e)
        {
            Exception ex = e;
            while (ex.InnerException != null)
                ex = ex.InnerException;
            return ex;
        }

        public static string GetFullMessage(this Exception exception)
        {
            string eMessage = string.Empty;
            while (exception != null)
            {
                eMessage += exception.Message;
                exception = exception.InnerException;
                if (exception != null)
                    eMessage += " Inner exception: ";
            }
            return eMessage;
        }
        #endregion
    }
}
