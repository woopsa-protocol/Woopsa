using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization.Json;

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

        //public object Deserialize(string json)
        //{
        //    var dictionnary = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        //    if (dictionnary != null)
        //        return dictionnary;

        //    var array = JsonConvert.DeserializeObject<object[]>(json);
        //    if (array != null)
        //        return array;

        //    return JsonConvert.DeserializeObject(json);
        //}


        public static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        //public string Serialize(object obj)
        //{
        //    var serializer = new DataContractJsonSerializer(obj.GetType());
        //    string json;
        //    using (var stream = new MemoryStream())
        //    {
        //        serializer.WriteObject(stream, obj);
        //        json = Encoding.UTF8.GetString(stream.ToArray());
        //    }

        //    return json;
        //}

        //public T Deserialize<T>(string json)
        //{
        //    object obj;
        //    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
        //    {
        //        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));

        //        obj = serializer.ReadObject(stream);
        //    }
        //    return (T)obj;
        //}


        //public object Deserialize(string json)
        //{
        //    object obj;
        //    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
        //    {
        //        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Dictionary<string, object>));

        //        obj = serializer.ReadObject(stream);
        //    }
        //    return obj;
        //}
    }
}
