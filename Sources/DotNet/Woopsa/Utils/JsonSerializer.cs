using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization.Json;
using System.IO;

namespace Woopsa
{
    public class JsonSerializer
    {
        public string Serialize(object obj)
        {
            var serializer = new DataContractJsonSerializer(obj.GetType());
            string json;
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, obj);
                json = Encoding.UTF8.GetString(stream.ToArray());
            }

            return json;
        }
        
        public T Deserialize<T>(string json) 
        {
            object obj;
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                obj = serializer.ReadObject(stream);
            }
            return (T)obj;
        }
    }
}
