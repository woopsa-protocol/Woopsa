#if UNITY_WSA

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Woopsa
{
    /// <summary>
    /// Json serializer for Unity when targeting UWP platforms. It uses a modified version
    /// of Newtonsoft adapted for Unity with the IL2CPP backend.
    /// JilleJr Newtonsoft for Unity => https://github.com/jilleJr/Newtonsoft.Json-for-Unity
    /// </summary>
    public static class UnityJsonSerializer
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

        public static T Deserialize<T>(string json, JsonConverter converter)
        {
            return JsonConvert.DeserializeObject<T>(json, converter);
        }
    }
}

#endif