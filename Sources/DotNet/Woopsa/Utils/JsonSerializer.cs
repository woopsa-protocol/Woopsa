using System.Collections.Generic;

#if NETCORE2 || NETSTANDARD2
        
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Woopsa
{
    public static class JsonSerializer
    {
        public static string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
        
        public static Dictionary<string, object> ToDictionnary(object obj)
        {
            var jobect = obj as JObject;
            if (jobect != null)
                return jobect.ToObject<Dictionary<string, object>>();

            return null;
        }

        public static object[] ToArray(object obj)
        {
            var jarray = obj as JArray;
            if (jarray != null)
                return jarray.ToObject<object[]>();

            return null;
        }
        
        public static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}

#else

using System.Web.Script.Serialization;

namespace Woopsa
{
    public static class JsonSerializer
    {
        public static string Serialize(object obj)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            return serializer.Serialize(obj);
        }

        public static Dictionary<string, object> ToDictionnary(object obj)
        {
            return (obj as Dictionary<string, object>);
        }

        public static object[] ToArray(object obj)
        {
            return obj as object[];
        }

        public static T Deserialize<T>(string json)
        {
            JavaScriptSerializer deserializer = new JavaScriptSerializer();
            return deserializer.Deserialize<T>(json);
        }
    }
}

#endif