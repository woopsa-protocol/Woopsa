using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.IO;
using System.Collections.Specialized;

namespace Woopsa
{
    public static class WoopsaMultiRequestConst
    {
        public const string WoopsaMultiRequestMethodName = "MultiRequest";
        public const string WoopsaMultiRequestArgumentName = "Requests";
    }

    public class WoopsaMultiRequestHandler
    {
        public WoopsaMultiRequestHandler(WoopsaObject root, WoopsaServer server)
        {
            _server = server;

            new WoopsaMethod(root, 
                WoopsaMultiRequestConst.WoopsaMultiRequestMethodName,
                WoopsaValueType.JsonData,
                new List<WoopsaMethodArgumentInfo>{new WoopsaMethodArgumentInfo(WoopsaMultiRequestConst.WoopsaMultiRequestArgumentName, WoopsaValueType.JsonData)},
                (s) => (HandleCall(s.ElementAt(0)))
            );
        }

        private WoopsaValue HandleCall(IWoopsaValue requestsArgument)
        {
            var serializer = new JavaScriptSerializer();
            Request[] requestsList = serializer.Deserialize<Request[]>(requestsArgument.AsText);
            List<MultipleRequestResponse> responses = new List<MultipleRequestResponse>();
            foreach(Request request in requestsList)
            {
                string result = null;
                if (request.Action.Equals("read"))
                    result = _server.ReadValue(request.Path);
                else if (request.Action.Equals("meta"))
                    result = _server.GetMetadata(request.Path);
                else if (request.Action.Equals("write"))
                    result = _server.WriteValue(request.Path, request.Value);
                else if (request.Action.Equals("invoke"))
                {
                    NameValueCollection argumentsAsCollection = request.Arguments.Aggregate(new NameValueCollection(), (collection, argument) =>
                    {
                        collection.Add(argument.Key, argument.Value);
                        return collection;
                    });
                    result = _server.InvokeMethod(request.Path, argumentsAsCollection);
                }
                MultipleRequestResponse response = new MultipleRequestResponse();
                response.Id = request.Id;
                response.Result = result;
                responses.Add(response);
            }
            return WoopsaValue.CreateUnchecked(responses.Serialize(), WoopsaValueType.JsonData);
        }

        private static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private WoopsaServer _server;
    }

    public class MultipleRequestResponse
    {
        public int Id { get; set; }
        public string Result { get; set; }
    }

    class Request
    {
        public int Id { get; set; }

        public string Action { get; set; }

        public string Path { get; set; }

        public string Value { get; set; }

        public Dictionary<string, string> Arguments { get; set; }
    }
}
