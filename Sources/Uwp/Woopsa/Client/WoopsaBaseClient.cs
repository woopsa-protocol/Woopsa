using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace Woopsa
{
    internal class WoopsaBaseClient
    {
        private readonly TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(10);

        public WoopsaBaseClient(string url)
        {
            if (!url.EndsWith(WoopsaConst.WoopsaPathSeparator.ToString()))
                url = url + WoopsaConst.WoopsaPathSeparator;
            _url = url;
        }

        public string Username { get; set; }
        public string Password { get; set; }

        public async Task<WoopsaValue> ReadAsync(string path)
        {
            string response = await RequestAsync("read" + path);
            return WoopsaValueFromResponse(response);
        }

        public async Task<WoopsaValue> WriteAsync(string path, string value)
        {
            NameValueCollection arguments = new NameValueCollection();
            arguments.Add(WoopsaFormat.KeyValue, value);
            string response = await RequestAsync("write" + path, arguments);
            return WoopsaValueFromResponse(response);
        }

        public async Task<WoopsaValue> Invoke(string path, NameValueCollection arguments, TimeSpan timeout)
        {
            string response = await RequestAsync("invoke" + path, arguments, timeout);
            return WoopsaValueFromResponse(response);
        }

        public async Task<WoopsaValue> Invoke(string path, NameValueCollection arguments)
        {
            return await Invoke(path, arguments, DefaultRequestTimeout);
        }

        public async Task<WoopsaMetaResult> MetaAsync(string path)
        {
            string response = await RequestAsync("meta" + path);
            return JsonConvert.DeserializeObject<WoopsaMetaResult>(response);
        }

        private WoopsaValue WoopsaValueFromResponse(string response)
        {
            //WoopsaReadResult result = JsonConvert.DeserializeObject<WoopsaReadResult>(response);
            WoopsaReadResult result = JsonConvert.DeserializeObject<WoopsaReadResult>(response, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple
            });

            if (result == null)
                return WoopsaValue.Null;

            // At this stage, the read result's type is still a string -- make it a WoopsaValueType
            WoopsaValueType valueType = (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), result.Type);

            if (valueType == WoopsaValueType.JsonData)
            {
                if (result.TimeStamp == null)
                    return new WoopsaValue(result.Value);
                else
                    return new WoopsaValue(result.Value, DateTime.Parse(result.TimeStamp));
            }
            else
            {
                if (result.TimeStamp == null)
                    return WoopsaValue.CreateChecked(result.Value.ToString(), (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), result.Type));
                else
                    return WoopsaValue.CreateChecked(result.Value.ToString(), (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), result.Type), DateTime.Parse(result.TimeStamp));
            }
            
        }

        private async Task<string> RequestAsync(string path, NameValueCollection postData, TimeSpan timeout)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_url + path);

            if (Username != null)
            {
                request.Credentials = new NetworkCredential(Username, Password);
            }

            request.ContinueTimeout = (int)timeout.TotalMilliseconds;
            if (postData != null)
            {
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            }
            else
            {
                request.Method = "GET";
            }
            request.Accept = "*/*";

            if (postData != null)
            {
                using (var writer = new StreamWriter(await request.GetRequestStreamAsync()))
                {
                    for (var i = 0; i < postData.Count; i++)
                    {
                        var key = postData.AllKeys[i];
                        if (i == postData.Count - 1)
                            writer.Write("{0}={1}", WebUtility.UrlEncode(key), WebUtility.UrlEncode(postData[key]));
                        else
                            writer.Write("{0}={1}&", WebUtility.UrlEncode(key), WebUtility.UrlEncode(postData[key]));
                    }
                }
            }

            HttpWebResponse response = null;
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
                throw new NotImplementedException();
                //if (response.ContentType == MIMETypes.Application.JSON)
                //{
                //    var serializer = new JavaScriptSerializer();
                //    WoopsaErrorResult error = serializer.Deserialize<WoopsaErrorResult>(resultString);

                //    // Generate one of the possible Woopsa exceptions based
                //    // on the JSON-serialized error 
                //    if (error.Type == typeof(WoopsaNotFoundException).Name)
                //        throw new WoopsaNotFoundException(error.Message);
                //    else if (error.Type == typeof(WoopsaNotificationsLostException).Name)
                //        throw new WoopsaNotificationsLostException(error.Message);
                //    else if (error.Type == typeof(WoopsaInvalidOperationException).Name)
                //        throw new WoopsaInvalidOperationException(error.Message);
                //    else if (error.Type == typeof(WoopsaInvalidSubscriptionChannelException).Name)
                //        throw new WoopsaInvalidSubscriptionChannelException(error.Message);
                //    else if (error.Type == typeof(WoopsaException).Name)
                //        throw new WoopsaException(error.Message);
                //    else
                //        throw new Exception(error.Message);
                //}
                //else
                //    throw new WoopsaException(response.StatusDescription);
            }
            return resultString;
        }

        private async Task<string> RequestAsync(string path)
        {
            return await RequestAsync(path, null);
        }

        private async Task<string> RequestAsync(string path, NameValueCollection postData)
        {
            return await RequestAsync(path, postData, DefaultRequestTimeout);
        }

        private string _url;

        //[JsonConverter(typeof(UserConverter)) ]
        private class WoopsaReadResult
        {
            public object Value { get; set; }

            public string Type { get; set; }

            public string TimeStamp { get; set; }
        }
    }

    //public class UserConverter : JsonConverter
    //{
    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //    {
    //    }

    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {
    //        return null;
    //    }

    //    public override bool CanConvert(Type objectType)
    //    {
    //        return true;
    //    }
    //}

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
