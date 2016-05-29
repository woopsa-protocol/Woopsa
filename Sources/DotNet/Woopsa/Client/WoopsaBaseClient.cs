<<<<<<< HEAD
﻿using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Web;
using System.Web.Script.Serialization;

namespace Woopsa
{
    internal class WoopsaBaseClient
    {
        #region Constructors

        public WoopsaBaseClient(string url)
        {
            if (!url.EndsWith(WoopsaConst.WoopsaPathSeparator.ToString()))
                url = url + WoopsaConst.WoopsaPathSeparator;
            _url = url;
        }

        #endregion

        #region Public Properties

        public string Username { get; set; }
        public string Password { get; set; }

        #endregion

        #region Public Methods

        public WoopsaValue Read(string path)
        {
            string response = Request("read" + path);
            return WoopsaValueFromResponse(response);
        }

        public WoopsaValue Write(string path, string value)
        {
            var arguments = new NameValueCollection { { WoopsaFormat.KeyValue, value } };
            string response = Request("write" + path, arguments);
            return WoopsaValueFromResponse(response);
        }

        public WoopsaValue Invoke(string path, NameValueCollection arguments, TimeSpan timeout)
        {
            string response = Request("invoke" + path, arguments, timeout);
            return WoopsaValueFromResponse(response);
        }

        public WoopsaValue Invoke(string path, NameValueCollection arguments)
        {
            return Invoke(path, arguments, _defaultRequestTimeout);
        }

        public WoopsaMetaResult Meta(string path)
        {
            string response = Request("meta" + path);
            var serializer = new JavaScriptSerializer();
            var result = serializer.Deserialize<WoopsaMetaResult>(response);
            return result;
        }

        #endregion

        #region Private Helpers

        private WoopsaValue WoopsaValueFromResponse(string response)
        {
            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            var result = serializer.Deserialize<WoopsaReadResult>(response);
            if (result != null)
            {
                var valueType = (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), result.Type);
                WoopsaValue resultWoopsaValue;
                DateTime? timeStamp;
                if (result.TimeStamp != null)
                    timeStamp = DateTime.Parse(result.TimeStamp, CultureInfo.InvariantCulture);
                else
                    timeStamp = null;
                if (valueType == WoopsaValueType.JsonData)
                    resultWoopsaValue = new WoopsaValue(WoopsaJsonData.CreateFromDeserializedData(result.Value), timeStamp);
                else
                    resultWoopsaValue = WoopsaValue.CreateChecked(WoopsaFormat.ToStringWoopsa(result.Value),
                        valueType, timeStamp);
                return resultWoopsaValue;
            }
            else
                return WoopsaValue.Null;
        }

        private string Request(string path, NameValueCollection postData = null)
        {
            return Request(path, postData, _defaultRequestTimeout);
        }

        private string Request(string path, NameValueCollection postData, TimeSpan timeout)
        {
            var request = (HttpWebRequest)WebRequest.Create(_url + path);

            if (Username != null)
                request.Credentials = new NetworkCredential(Username, Password);

            request.Timeout = (int)timeout.TotalMilliseconds;

            if (postData != null)
            {
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            }
            else
                request.Method = "GET";

            request.Accept = "*/*";

            if (postData != null)
            {
                using (var writer = new StreamWriter(request.GetRequestStream()))
                {
                    for (var i = 0; i < postData.Count; i++)
                    {
                        string key = postData.AllKeys[i];
                        writer.Write(i == postData.Count - 1 ? "{0}={1}" : "{0}={1}&", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(postData[key]));
                    }
                }
            }

            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException exception)
            {
                // This could be an HTTP error, in which case
                // we actually have a response (with the HTTP 
                // status and error)
                response = (HttpWebResponse)exception.Response;
                if (response == null)
                {
                    // Sometimes, we can make the request, but the server dies
                    // before we get a reply - in that case the Response
                    // is null, so we re-throw the exception
                    throw;
                }
            }

            string resultString;
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                resultString = reader.ReadToEnd();
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (response.ContentType == MIMETypes.Application.JSON)
                {
                    var serializer = new JavaScriptSerializer();
                    var error = serializer.Deserialize<WoopsaErrorResult>(resultString);

                    // Generate one of the possible Woopsa exceptions based
                    // on the JSON-serialized error
                    if (error.Type == typeof(WoopsaNotFoundException).Name)
                        throw new WoopsaNotFoundException(error.Message);
                    if (error.Type == typeof(WoopsaNotificationsLostException).Name)
                        throw new WoopsaNotificationsLostException(error.Message);
                    if (error.Type == typeof(WoopsaInvalidOperationException).Name)
                        throw new WoopsaInvalidOperationException(error.Message);
                    if (error.Type == typeof(WoopsaInvalidSubscriptionChannelException).Name)
                        throw new WoopsaInvalidSubscriptionChannelException(error.Message);
                    if (error.Type == typeof(WoopsaException).Name)
                        throw new WoopsaException(error.Message);
                    throw new Exception(error.Message);
                }

                throw new WoopsaException(response.StatusDescription);
            }

            return resultString;
        }

        #endregion

        #region Private Members

        private readonly TimeSpan _defaultRequestTimeout = TimeSpan.FromSeconds(10);

        private readonly string _url;

        #endregion

        #region Private Nested Classes

        private class WoopsaReadResult
        {
            public object Value { get; set; }
            public string Type { get; set; }
            public string TimeStamp { get; set; }
        }

        #endregion
    }

    public class WoopsaErrorResult
    {
        public string Type { get; set; }
        public string Message { get; set; }
    }

    public class WoopsaMetaResult
    {
        public string Name { get; set; }
        public string[] Items { get; set; }
        public WoopsaPropertyMeta[] Properties { get; set; }
        public WoopsaMethodMeta[] Methods { get; set; }
    }

    public class WoopsaPropertyMeta
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool ReadOnly { get; set; }
    }

    public class WoopsaMethodMeta
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public WoopsaMethodArgumentInfoMeta[] ArgumentInfos { get; set; }
    }

    public class WoopsaMethodArgumentInfoMeta
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public class WoopsaNotificationsEventArgs : EventArgs
    {
        public WoopsaNotificationsEventArgs(IWoopsaNotifications notifications)
        {
            Notifications = notifications;
        }

        public IWoopsaNotifications Notifications { get; set; }
    }
}
=======
﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Web;
using System.Web.Script.Serialization;

namespace Woopsa
{
    internal class WoopsaBaseClient : IDisposable
    {
        #region Constructors

        public WoopsaBaseClient(string url)
        {
            _pendingRequests = new List<WebRequest>();
            if (!url.EndsWith(WoopsaConst.WoopsaPathSeparator.ToString()))
                url = url + WoopsaConst.WoopsaPathSeparator;
            _url = url;
        }

        #endregion

        #region Public Properties

        public string Username { get; set; }
        public string Password { get; set; }

        #endregion

        #region Public Methods

        public WoopsaValue Read(string path)
        {
            string response = Request("read" + path);
            return WoopsaValueFromResponse(response);
        }

        public WoopsaValue Write(string path, string value)
        {
            var arguments = new NameValueCollection { { WoopsaFormat.KeyValue, value } };
            string response = Request("write" + path, arguments);
            return WoopsaValueFromResponse(response);
        }

        public WoopsaValue Invoke(string path, NameValueCollection arguments, TimeSpan timeout)
        {
            string response = Request("invoke" + path, arguments, timeout);
            return WoopsaValueFromResponse(response);
        }

        public WoopsaValue Invoke(string path, NameValueCollection arguments)
        {
            return Invoke(path, arguments, _defaultRequestTimeout);
        }

        public WoopsaMetaResult Meta(string path)
        {
            string response = Request("meta" + path);
            var serializer = new JavaScriptSerializer();
            var result = serializer.Deserialize<WoopsaMetaResult>(response);
            return result;
        }

        public void Terminate()
        {
            _terminating = true;
            AbortPendingRequests();

        }

        #endregion

        #region Private Helpers

        private WoopsaValue WoopsaValueFromResponse(string response)
        {
            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            var result = serializer.Deserialize<WoopsaReadResult>(response);
            if (result != null)
            {
                var valueType = (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), result.Type);
                WoopsaValue resultWoopsaValue;
                DateTime? timeStamp;
                if (result.TimeStamp != null)
                    timeStamp = DateTime.Parse(result.TimeStamp, CultureInfo.InvariantCulture);
                else
                    timeStamp = null;
                if (valueType == WoopsaValueType.JsonData)
                    resultWoopsaValue = new WoopsaValue(WoopsaJsonData.CreateFromDeserializedData(result.Value), timeStamp);
                else
                    resultWoopsaValue = WoopsaValue.CreateChecked(WoopsaFormat.ToStringWoopsa(result.Value),
                        valueType, timeStamp);
                return resultWoopsaValue;
            }
            else
                return WoopsaValue.Null;
        }

        private string Request(string path, NameValueCollection postData = null)
        {
            return Request(path, postData, _defaultRequestTimeout);
        }

        private string Request(string path, NameValueCollection postData, TimeSpan timeout)
        {
            if (!_terminating)
            {
                var request = (HttpWebRequest)WebRequest.Create(_url + path);
                lock (_pendingRequests)
                    _pendingRequests.Add(request);
                try
                {

                    if (Username != null)
                        request.Credentials = new NetworkCredential(Username, Password);

                    request.Timeout = (int)timeout.TotalMilliseconds;

                    if (postData != null)
                    {
                        request.Method = "POST";
                        request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                    }
                    else
                        request.Method = "GET";

                    request.Accept = "*/*";

                    if (postData != null)
                    {
                        using (var writer = new StreamWriter(request.GetRequestStream()))
                        {
                            for (var i = 0; i < postData.Count; i++)
                            {
                                string key = postData.AllKeys[i];
                                writer.Write(i == postData.Count - 1 ? "{0}={1}" : "{0}={1}&", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(postData[key]));
                            }
                        }
                    }

                    HttpWebResponse response;
                    try
                    {
                        response = (HttpWebResponse)request.GetResponse();
                    }
                    catch (WebException exception)
                    {
                        // This could be an HTTP error, in which case
                        // we actually have a response (with the HTTP 
                        // status and error)
                        response = (HttpWebResponse)exception.Response;
                        if (response == null)
                        {
                            // Sometimes, we can make the request, but the server dies
                            // before we get a reply - in that case the Response
                            // is null, so we re-throw the exception
                            throw;
                        }
                    }

                    string resultString;
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        resultString = reader.ReadToEnd();
                    }

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        if (response.ContentType == MIMETypes.Application.JSON)
                        {
                            var serializer = new JavaScriptSerializer();
                            var error = serializer.Deserialize<WoopsaErrorResult>(resultString);

                            // Generate one of the possible Woopsa exceptions based
                            // on the JSON-serialized error
                            if (error.Type == typeof(WoopsaNotFoundException).Name)
                                throw new WoopsaNotFoundException(error.Message);
                            if (error.Type == typeof(WoopsaNotificationsLostException).Name)
                                throw new WoopsaNotificationsLostException(error.Message);
                            if (error.Type == typeof(WoopsaInvalidOperationException).Name)
                                throw new WoopsaInvalidOperationException(error.Message);
                            if (error.Type == typeof(WoopsaInvalidSubscriptionChannelException).Name)
                                throw new WoopsaInvalidSubscriptionChannelException(error.Message);
                            if (error.Type == typeof(WoopsaException).Name)
                                throw new WoopsaException(error.Message);
                            throw new Exception(error.Message);
                        }

                        throw new WoopsaException(response.StatusDescription);
                    }

                    return resultString;
                }
                finally
                {
                    lock (_pendingRequests)
                        _pendingRequests.Remove(request);
                }
            }
            else
                throw new ObjectDisposedException(GetType().Name);
        }

        private void AbortPendingRequests()
        {
            WebRequest[] pendingRequests;
            lock (_pendingRequests)
                pendingRequests = _pendingRequests.ToArray();
            foreach (var item in pendingRequests)
                item.Abort();
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                Terminate();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private Members

        private readonly TimeSpan _defaultRequestTimeout = TimeSpan.FromSeconds(10);

        private readonly string _url;

        private List<WebRequest> _pendingRequests;
        private bool _terminating;
        #endregion

        #region Private Nested Classes

        private class WoopsaReadResult
        {
            public object Value { get; set; }
            public string Type { get; set; }
            public string TimeStamp { get; set; }
        }

        #endregion
    }

    public class WoopsaErrorResult
    {
        public string Type { get; set; }
        public string Message { get; set; }
    }

    public class WoopsaMetaResult
    {
        public string Name { get; set; }
        public string[] Items { get; set; }
        public WoopsaPropertyMeta[] Properties { get; set; }
        public WoopsaMethodMeta[] Methods { get; set; }
    }

    public class WoopsaPropertyMeta
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool ReadOnly { get; set; }
    }

    public class WoopsaMethodMeta
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public WoopsaMethodArgumentInfoMeta[] ArgumentInfos { get; set; }
    }

    public class WoopsaMethodArgumentInfoMeta
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public class WoopsaNotificationsEventArgs : EventArgs
    {
        public WoopsaNotificationsEventArgs(IWoopsaNotifications notifications)
        {
            Notifications = notifications;
        }

        public IWoopsaNotifications Notifications { get; set; }
    }
}
>>>>>>> refs/remotes/woopsa-protocol/master
