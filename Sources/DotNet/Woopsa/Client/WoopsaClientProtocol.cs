using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Woopsa
{
    public class ClientRequestEventArgs : EventArgs
    {
        public ClientRequestEventArgs(string path, NameValueCollection postData)
        {
            Path = path;
            PostData = postData;
        }

        public string Path { get; }

        public NameValueCollection PostData { get; }
    }

    public class WoopsaClientProtocol : IDisposable
    {
        #region Constructors

        public WoopsaClientProtocol(string url)
        {
            Uri uri = new Uri(url);
            ServicePoint servicePoint = ServicePointManager.FindServicePoint(uri);
           servicePoint.Expect100Continue = false;
            servicePoint.UseNagleAlgorithm = false;
            _credentialsChanged = true;

            if (!url.EndsWith(WoopsaConst.WoopsaPathSeparator.ToString()))
                url = url + WoopsaConst.WoopsaPathSeparator;
            Url = url;
        }

        #endregion

        #region Public Properties

        public string Url { get; }

        public string Username
        {
            get => _userName;
            set
            {
                if (_userName != value)
                {
                    _userName = value;
                    _credentialsChanged = true;
                }
            }
        }

        private string _userName;

        public string Password
        {
            get => _password;
            set
            {
                if (_password != value)
                {
                    _password = value;
                    _credentialsChanged = true;
                }
            }
        }

        private string _password;

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
            CancelPendingRequests();
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

        private HttpClient CurrentHttpClient
        {
            get
            {
                HttpClient result = _httpClient;
                if (_credentialsChanged)
                    lock (_httpClientLocker)
                    {
                        var previousHttpClient = _httpClient;
                        _httpClient?.CancelPendingRequests();
                        var httpClientHandler = new HttpClientHandler();

                        _credentialsChanged = false;
                        if (Username != null)
                            httpClientHandler.Credentials = new NetworkCredential(Username, Password);
                        _httpClient = new HttpClient(httpClientHandler, true);
                        previousHttpClient?.Dispose();
                        result = _httpClient;
                    }
                return result;
            }
        }

        private static IEnumerable<KeyValuePair<string, string>> ToPairs(NameValueCollection collection)
        {
            return collection.AllKeys.Select(key => new KeyValuePair<string, string>(key, collection[key]));
        }

        private async Task<string> RequestAsync(string path, NameValueCollection postData, TimeSpan timeout)
        {
            if (!_terminating)
            {
                HttpClient client = CurrentHttpClient;
                var cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(timeout);
                HttpResponseMessage response = null;
                try
                {
                    try
                    {
                        var requestUrl = Url + path;
                        if (postData == null)
                            response = await client.GetAsync(requestUrl, HttpCompletionOption.ResponseHeadersRead, cancellationTokenSource.Token);
                        else
                        {
                            HttpContent content = new FormUrlEncodedContent(ToPairs(postData));
                            response = await client.PostAsync(requestUrl, content, cancellationTokenSource.Token);
                        }
                        SetLastCommunicationSuccessful(true);
                    }
                    catch (Exception)
                    {
                        SetLastCommunicationSuccessful(false);
                        throw;
                    }

                    var resultString = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        if (response.Content.Headers.ContentType.MediaType == MIMETypes.Application.JSON)
                        {
                            var exception = WoopsaFormat.DeserializeError(resultString);
                            throw exception;
                        }

                        throw new WoopsaException(response.ReasonPhrase);
                    }

                    return resultString;
                }
                finally
                {
                    response?.Dispose();
                }
            }
            else
                throw new ObjectDisposedException(GetType().Name);
        }

        private string Request(string path, NameValueCollection postData, TimeSpan timeout)
        {
            try
            {
                return RequestAsync(path, postData, timeout).Result;
            }
            catch (AggregateException e)
            {
                if (e.InnerExceptions.Count == 1)
                    throw e.InnerException;
                else
                    throw;
            }
        }
        private void CancelPendingRequests() => _httpClient.CancelPendingRequests();

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Terminate();
                _httpClient?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private Members

        private HttpClient _httpClient;
        private object _httpClientLocker = new object();
        private bool _credentialsChanged;

        private readonly TimeSpan _defaultRequestTimeout = TimeSpan.FromSeconds(10);

        private bool _terminating;


        #endregion
    }

}
