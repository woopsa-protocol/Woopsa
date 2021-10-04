using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Woopsa
{
    static public class WoopsaUtils
    {
        #region Public Static Methods

        public static string CombinePath(string basePath, string relativePath)
        {
            return basePath.TrimEnd(WoopsaConst.WoopsaPathSeparator) +
                WoopsaConst.WoopsaPathSeparator +
                RemoveInitialSeparator(relativePath);
        }

        public static string CombineUrl(string baseUrl, params string[] relativeUrls)
        {
            string url = baseUrl;
            foreach (var item in relativeUrls)
                url = CombineSingleUrl(url, item);
            return url;
        }

        public static string RemoveInitialAndFinalSeparator(string path)
        {
            return RemoveFinalSeparator(RemoveInitialSeparator(path));
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

        public static string RemoveFinalSeparator(string path)
        {
            if (path.Length >= 1)
                if (path.Last() == WoopsaConst.WoopsaPathSeparator)
                    return path.Remove(path.Length - 1);
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

        #endregion

        #region Private Static fields / Attributes

        private static UInt64 _instanceIndex;
        private static object _instanceIndexLock = new object();

        public static JsonSerializerOptions ObjectToInferredTypesConverterOptions = new JsonSerializerOptions
        {
            Converters = { new ObjectToInferredTypesConverter() }
        };

        #endregion

        #region Private Static Methods

        private static string CombineSingleUrl(string baseUrl, string relativeUrl)
        {
            if (String.IsNullOrEmpty(baseUrl)) return relativeUrl;
            if (String.IsNullOrEmpty(relativeUrl)) return baseUrl;
            return baseUrl.TrimEnd('/') + "/" + relativeUrl.TrimStart('/');
        }

        #endregion

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
