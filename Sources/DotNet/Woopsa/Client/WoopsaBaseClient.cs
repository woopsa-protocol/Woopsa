using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

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

        public event EventHandler<WoopsaNotificationsEventArgs> PropertyChange;

        public WoopsaValue Read(string path)
        {
            string response = Request("read" + path);
            return WoopsaValueFromResponse(response);
        }

        public WoopsaValue Write(string path, string value)
        {
            NameValueCollection arguments = new NameValueCollection();
            arguments.Add(WoopsaFormat.KeyValue, value);
            string response = Request("write" + path, arguments);
            return WoopsaValueFromResponse(response);
        }

        public WoopsaValue Invoke(string path, NameValueCollection arguments, int timeout)
        {
            string response = Request("invoke" + path, arguments, timeout);
            return WoopsaValueFromResponse(response);
        }

        public WoopsaValue Invoke(string path, NameValueCollection arguments)
        {
            return Invoke(path, arguments, (int)DefaultRequestTimeout.TotalMilliseconds);
        }

        public WoopsaMetaResult Meta(string path)
        {
            string response = Request("meta" + path);
            var serializer = new JavaScriptSerializer();
            var result = serializer.Deserialize<WoopsaMetaResult>(response);
            return result;
        }

        public void Subscribe(string path)
        {
            if (_subscriptionChannel == null)
            {
                // TODO: fallback if no subscription service 
                if (!_hasSubscriptionService.HasValue)
                {
                    // TODO : Check if this is supported on the server
                    _hasSubscriptionService = true;
                }
                if (_hasSubscriptionService == true)
                    _subscriptionChannel = new WoopsaClientSubscriptionChannel(this);
                else
                    _subscriptionChannel = new WoopsaClientSubscriptionChannelFallback(this);

                _subscriptionChannel.ValueChange +=_subscriptionChannel_ValueChange;
            }
            _subscriptionChannel.Register(path);
        }

        public void Unsubscribe(string path)
        {
            if (_subscriptionChannel != null)
            {
                _subscriptionChannel.Unregister(path);
            }
        }

        private void _subscriptionChannel_ValueChange(object sender, WoopsaNotificationsEventArgs notifications)
        {
            DoPropertyChanged(notifications);
        }

        protected virtual void DoPropertyChanged(WoopsaNotificationsEventArgs notifications)
        {
            if (PropertyChange != null)
            {
                PropertyChange(this, notifications);
            }
        }

        private WoopsaValue WoopsaValueFromResponse(string response)
        {
            var serializer = new JavaScriptSerializer();
            WoopsaReadResult result = serializer.Deserialize<WoopsaReadResult>(response);

            if ((WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), result.Type) == WoopsaValueType.JsonData)
            {
                if (result.TimeStamp == null)
                    return new WoopsaValue(result.Value);
                else
                    return new WoopsaValue(result.Value, DateTime.Parse(result.TimeStamp));
            }
            else
            {
                if (result.TimeStamp == null)
                    return new WoopsaValue(result.Value.ToString(), (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), result.Type));
                else
                    return new WoopsaValue(result.Value.ToString(), (WoopsaValueType)Enum.Parse(typeof(WoopsaValueType), result.Type), DateTime.Parse(result.TimeStamp));
            }
        }
        
        private string Request(string path, NameValueCollection postData, int timeout)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_url + path);

            if (Username != null)
            {
                request.Credentials = new NetworkCredential(Username, Password);
            }

            request.Timeout = timeout;
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
                using( var writer = new StreamWriter(request.GetRequestStream()) )
                {
                    for (var i = 0; i < postData.Count; i++)
                    {
                        var key = postData.AllKeys[i];
                        if (i == postData.Count - 1)
                            writer.Write("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(postData[key]));
                        else
                            writer.Write("{0}={1}&", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(postData[key]));
                    }
                }
            }

            var response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
                throw new WoopsaException(response.StatusDescription);

            string result;
            using ( var reader = new StreamReader(response.GetResponseStream()) )
            {
                result = reader.ReadToEnd();
            }
            return result;
        }

        private string Request(string path)
        {
            return Request(path, null);
        }

        private string Request(string path, NameValueCollection postData)
        {
            return Request(path, postData, (int)DefaultRequestTimeout.TotalMilliseconds);
        }

        private string _url;
        private WoopsaClientSubscriptionChannelBase _subscriptionChannel;
        private bool? _hasSubscriptionService = null;

        private class WoopsaReadResult
        {
            public object Value { get; set; }
            public string Type { get; set; }
            public string TimeStamp { get; set; }
        }
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
