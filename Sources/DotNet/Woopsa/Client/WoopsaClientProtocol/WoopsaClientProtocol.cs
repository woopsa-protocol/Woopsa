using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Woopsa
{
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
            _httpClient = new HttpClient { BaseAddress = uri };
        }

        #endregion

        #region Public Properties

        public string Url { get; private set; }

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
            return ReadAsync(path).Result;
        }

        public async Task<WoopsaValue> ReadAsync(string path)
        {
            return await RequestAndDeserializeAsync(WoopsaFormat.VerbRead + WoopsaConst.WoopsaPathSeparator + WoopsaUtils.RemoveInitialSeparator(path), WoopsaFormat.DeserializeWoopsaValue);
        }

        public WoopsaValue Write(string path, string value)
        {
            return WriteAsync(path, value).Result;
        }

        public async Task<WoopsaValue> WriteAsync(string path, string value)
        {
            var arguments = new NameValueCollection { { WoopsaFormat.KeyValue, value } };
            return await RequestAndDeserializeAsync(WoopsaFormat.VerbWrite + WoopsaConst.WoopsaPathSeparator + WoopsaUtils.RemoveInitialSeparator(path), WoopsaFormat.DeserializeWoopsaValue, arguments);
        }

        public WoopsaValue Invoke(string path, NameValueCollection arguments, TimeSpan timeout)
        {
            return InvokeAsync(path, arguments, timeout).Result;
        }

        public async Task<WoopsaValue> InvokeAsync(string path, NameValueCollection arguments, TimeSpan timeout)
        {
            return await RequestAndDeserializeAsync(WoopsaFormat.VerbInvoke + WoopsaConst.WoopsaPathSeparator + WoopsaUtils.RemoveInitialSeparator(path), WoopsaFormat.DeserializeWoopsaValue, arguments, timeout);
        }

        public WoopsaValue Invoke(string path, NameValueCollection arguments)
        {
            return Invoke(path, arguments, _defaultRequestTimeout);
        }

        public async Task<WoopsaValue> InvokeAsync(string path, NameValueCollection arguments)
        {
            return await InvokeAsync(path, arguments, _defaultRequestTimeout);
        }

        public WoopsaMetaResult Meta(string path)
        {
            return MetaAsync(path).Result;
        }

        public async Task<WoopsaMetaResult> MetaAsync(string path = "")
        {
            return await RequestAndDeserializeAsync(WoopsaFormat.VerbMeta + WoopsaConst.UrlSeparator + WoopsaUtils.RemoveInitialSeparator(path), WoopsaFormat.DeserializeMeta);
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

        private async Task<T> RequestAndDeserializeAsync<T>(string path, Func<string, T> deserializer, NameValueCollection postData = null)
        {
            return await RequestAndDeserializeAsync(path, deserializer, postData, _defaultRequestTimeout);
        }

        private async Task<T> RequestAndDeserializeAsync<T>(string path, Func<string, T> deserializer, NameValueCollection postData, TimeSpan timeout)
        {
            var eventArgs = new ClientRequestEventArgs(path, postData);
            OnBeforeClientRequest(eventArgs);
            try
            {
                string response = await RequestAsync(path, postData, timeout);
                return deserializer(response);
            }
            finally
            {
                OnAfterClientRequest(eventArgs);
            }
        }

        private static IEnumerable<KeyValuePair<string, string>> ToPairs(NameValueCollection collection)
        {
            return collection.AllKeys.Select(key => new KeyValuePair<string, string>(key, collection[key]));
        }

        private string authData = null;

        private HttpRequestMessage CurrentHttpRequestMessage
        {
            get
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage();
                if (_credentialsChanged)
                {
                    lock (_httpClientLocker)
                    {
                        _credentialsChanged = false;
                        if (Username is not null)
                        {
                            authData = EncodeBase64($"{_userName}:{_password}");
                        }
                    }
                }
                if (authData is not null)
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", authData);
                }
                return requestMessage;
            }
        }

        public string EncodeBase64(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        private async Task<string> RequestAsync(string path, NameValueCollection postData, TimeSpan timeout)
        {
            if (!_terminating)
            {
                path = WoopsaUtils.RemoveInitialAndFinalSeparator(path);

                HttpResponseMessage response = null;
                HttpClient client = _httpClient;
                HttpRequestMessage req = CurrentHttpRequestMessage;
                var cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(timeout);
                try
                {
                    try
                    {
                        req.RequestUri = new Uri(Url + path);
                        if (postData == null)
                        {
                            req.Method = System.Net.Http.HttpMethod.Get;
                            response = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationTokenSource.Token);
                        }
                        else
                        {
                            req.Method = System.Net.Http.HttpMethod.Post;
                            req.Content = new FormUrlEncodedContent(ToPairs(postData));
                            response = await client.SendAsync(req, cancellationTokenSource.Token);
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
                        if (response.Content.Headers.ContentType != null && response.Content.Headers.ContentType.MediaType == MIMETypes.Application.JSON)
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

        private readonly TimeSpan _defaultRequestTimeout = TimeSpan.FromSeconds(30);

        private bool _terminating;

        #endregion
    }

}
