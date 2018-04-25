using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Web;

namespace Woopsa
{
    public class ClientRequestEventArgs : EventArgs
    {
        public ClientRequestEventArgs(string path, NameValueCollection postData)
        {
            Path = path;
            PostData = postData;
        }

        public string Path { get; private set; }

        public NameValueCollection PostData { get; private set; }
    }

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
            return RequestAndDeserialize(WoopsaFormat.VerbRead + path, WoopsaFormat.DeserializeWoopsaValue);
        }

        public WoopsaValue Write(string path, string value)
        {
            var arguments = new NameValueCollection { { WoopsaFormat.KeyValue, value } };
            return RequestAndDeserialize(WoopsaFormat.VerbWrite + path, WoopsaFormat.DeserializeWoopsaValue, arguments);
        }

        public WoopsaValue Invoke(string path, NameValueCollection arguments, TimeSpan timeout)
        {
            return RequestAndDeserialize(WoopsaFormat.VerbInvoke + path, WoopsaFormat.DeserializeWoopsaValue, arguments, timeout);
        }

        public WoopsaValue Invoke(string path, NameValueCollection arguments)
        {
            return Invoke(path, arguments, _defaultRequestTimeout);
        }

        public WoopsaMetaResult Meta(string path)
        {
            return RequestAndDeserialize(WoopsaFormat.VerbMeta + path, WoopsaFormat.DeserializeMeta);
        }

        public void Terminate()
        {
            _terminating = true;
            AbortPendingRequests();
        }
        #endregion

        #region Is Last Communication successfull
        public bool IsLastCommunicationSuccessful => _isLastCommunicationSuccessful;

        private void SetLastCommunicationSuccessful(bool value)
        {
            if (value != _isLastCommunicationSuccessful || !_hasValueIsLastCommunicationSuccessful)
            {
                _isLastCommunicationSuccessful = value;
                _hasValueIsLastCommunicationSuccessful = true;
                OnIsLastCommunicationSuccessfulChange();
            }
        }

        public event EventHandler IsLastCommunicationSuccessfulChange;

        protected virtual void OnIsLastCommunicationSuccessfulChange()
        {
            IsLastCommunicationSuccessfulChange?.Invoke(this, new EventArgs());
        }

        private volatile bool _isLastCommunicationSuccessful;
        private volatile bool _hasValueIsLastCommunicationSuccessful;
        #endregion

        #region Before / After client request event

        public event EventHandler<ClientRequestEventArgs> BeforeClientRequest;
        public event EventHandler<ClientRequestEventArgs> AfterClientRequest;

        protected virtual void OnBeforeClientRequest(ClientRequestEventArgs args)
        {
            BeforeClientRequest?.Invoke(this, args);
        }

        protected virtual void OnAfterClientRequest(ClientRequestEventArgs args)
        {
            AfterClientRequest?.Invoke(this, args);
        }

        #endregion

        #region Private Helpers
        private T RequestAndDeserialize<T>(string path, Func<string, T> deserializer, NameValueCollection postData = null)
        {
            return RequestAndDeserialize(path, deserializer, postData, _defaultRequestTimeout);
        }

        private T RequestAndDeserialize<T>(string path, Func<string, T> deserializer, NameValueCollection postData, TimeSpan timeout)
        {
            var eventArgs = new ClientRequestEventArgs(path, postData);
            OnBeforeClientRequest(eventArgs);
            try
            {
                string response = Request(path, postData, timeout);
                return deserializer(response);
            }
            finally
            {
                OnAfterClientRequest(eventArgs);
            }
        }

        private string Request(string path, NameValueCollection postData, TimeSpan timeout)
        {
            if (!_terminating)
            {
                var request = (HttpWebRequest)WebRequest.Create(Url + path);
                request.CachePolicy = _cachePolicy;

                // TODO : affiner l'optimisation de performance
                request.ServicePoint.UseNagleAlgorithm = false;
                request.ServicePoint.Expect100Continue = false;

                HttpWebResponse response = null;

                lock (_pendingRequests)
                    _pendingRequests.Add(request);
                try
                {
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

                        response = (HttpWebResponse)request.GetResponse();
                        SetLastCommunicationSuccessful(true);
                    }
                    catch (WebException exception)
                    {
                        // This could be an HTTP error, in which case
                        // we actually have a response (with the HTTP 
                        // status and error)
                        response = (HttpWebResponse)exception.Response;
                        if (response == null)
                        {
                            SetLastCommunicationSuccessful(false);
                            // Sometimes, we can make the request, but the server dies
                            // before we get a reply - in that case the Response
                            // is null, so we re-throw the exception
                            throw;
                        }
                    }
                    catch (Exception)
                    {
                        SetLastCommunicationSuccessful(false);
                        throw;
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
                    if (response != null)
                        response.Close();
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

        static private HttpRequestCachePolicy _cachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);

        #endregion

        #region Private Nested Classes

        #endregion
    }

}
