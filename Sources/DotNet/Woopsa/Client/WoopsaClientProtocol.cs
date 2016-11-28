using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

namespace Woopsa
{
    public class WoopsaClientProtocol : IDisposable
    {
        #region Constructors

        public WoopsaClientProtocol(string url)
        {
            _pendingRequests = new List<WebRequest>();
            if (!url.EndsWith(WoopsaConst.WoopsaPathSeparator.ToString()))
                url = url + WoopsaConst.WoopsaPathSeparator;
            Url = url;
        }

        #endregion

        #region Public Properties

        public string Url { get; private set; }
        public string Username { get; set; }
        public string Password { get; set; }

        #endregion

        #region Public Methods

        public WoopsaValue Read(string path)
        {
            string response = Request(WoopsaFormat.VerbRead + path);
            return WoopsaFormat.DeserializeWoopsaValue(response);
        }

        public WoopsaValue Write(string path, string value)
        {
            var arguments = new NameValueCollection { { WoopsaFormat.KeyValue, value } };
            string response = Request(WoopsaFormat.VerbWrite + path, arguments);
            return WoopsaFormat.DeserializeWoopsaValue(response);
        }

        public WoopsaValue Invoke(string path, NameValueCollection arguments, TimeSpan timeout)
        {
            string response = Request(WoopsaFormat.VerbInvoke + path, arguments, timeout);
            return WoopsaFormat.DeserializeWoopsaValue(response);
        }

        public WoopsaValue Invoke(string path, NameValueCollection arguments)
        {
            return Invoke(path, arguments, _defaultRequestTimeout);
        }

        public WoopsaMetaResult Meta(string path)
        {
            string response = Request(WoopsaFormat.VerbMeta + path);
            var result = WoopsaFormat.DeserializeMeta(response);
            return result;
        }

        public void Terminate()
        {
            _terminating = true;
            AbortPendingRequests();
        }

        #endregion

        #region Private Helpers


        private string Request(string path, NameValueCollection postData = null)
        {
            return Request(path, postData, _defaultRequestTimeout);
        }

        private string Request(string path, NameValueCollection postData, TimeSpan timeout)
        {
            if (!_terminating)
            {
                var request = (HttpWebRequest)WebRequest.Create(Url + path);

                // TODO : enlever
                request.ServicePoint.UseNagleAlgorithm = false;
                request.ServicePoint.Expect100Continue = false;

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
                        StringBuilder stringBuilder = new StringBuilder();
                        for (var i = 0; i < postData.Count; i++)
                        {
                            string key = postData.AllKeys[i];
                             stringBuilder.AppendFormat(i == postData.Count - 1 ? "{0}={1}" : "{0}={1}&", 
                                 HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(postData[key]));
                        }                        
                        using (var writer = new StreamWriter(request.GetRequestStream()))
                            writer.Write(stringBuilder.ToString());
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
                            var exception = WoopsaFormat.DeserializeError(resultString);
                            throw exception;
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

        private List<WebRequest> _pendingRequests;
        private bool _terminating;

        #endregion

        #region Private Nested Classes

        #endregion
    }

}
