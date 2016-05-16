using Newtonsoft.Json;
using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters;
using System.Threading.Tasks;

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
        public async Task<WoopsaValue> ReadAsync(string path)
        {
            string response = await RequestAsync("read" + path);
            return WoopsaValueFromResponse(response);
        }

        public async Task<WoopsaValue> WriteAsync(string path, string value)
        {
            var arguments = new NameValueCollection { { WoopsaFormat.KeyValue, value } };
            string response = await RequestAsync("write" + path, arguments);
            return WoopsaValueFromResponse(response);
        }

        public async Task<WoopsaValue> InvokeAsync(string path, NameValueCollection arguments, TimeSpan timeout)
        {
            string response = await RequestAsync("invoke" + path, arguments, timeout);
            return WoopsaValueFromResponse(response);
        }

        public async Task<WoopsaValue> InvokeAsync(string path, NameValueCollection arguments)
        {
            return await InvokeAsync(path, arguments, _defaultRequestTimeout);
        }

        public async Task<WoopsaMetaResult> MetaAsync(string path)
        {
            string response = await RequestAsync("meta" + path);
            return JsonConvert.DeserializeObject<WoopsaMetaResult>(response);
        }

        #endregion

        #region Private Helpers

        private WoopsaValue WoopsaValueFromResponse(string response)
        {
            WoopsaReadResult result = JsonConvert.DeserializeObject<WoopsaReadResult>(response, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple
            });

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

        private async Task<string> RequestAsync(string path, NameValueCollection postData = null)
        {
            return await RequestAsync(path, postData, _defaultRequestTimeout);
        }

        private async Task<string> RequestAsync(string path, NameValueCollection postData, TimeSpan timeout)
        {
            var request = (HttpWebRequest)WebRequest.Create(_url + path);

            if (Username != null)
                request.Credentials = new NetworkCredential(Username, Password);

            request.ContinueTimeout = (int)timeout.TotalMilliseconds;

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
                using (var writer = new StreamWriter(await request.GetRequestStreamAsync()))
                {
                    for (var i = 0; i < postData.Count; i++)
                    {
                        string key = postData.AllKeys[i];
                        writer.Write(i == postData.Count - 1 ? "{0}={1}" : "{0}={1}&", WebUtility.UrlEncode(key), WebUtility.UrlEncode(postData[key]));
                    }
                }
            }

            HttpWebResponse response;
            try
            {
                response = await request.GetResponseAsync() as HttpWebResponse;
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
                var error = JsonConvert.DeserializeObject<WoopsaErrorResult>(resultString);
                if (error != null)
                {
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
