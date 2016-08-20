using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.IO;
using System.Collections.Specialized;
using System.Diagnostics;

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
                new List<WoopsaMethodArgumentInfo> { new WoopsaMethodArgumentInfo(WoopsaMultiRequestConst.WoopsaMultiRequestArgumentName, WoopsaValueType.JsonData) },
                (s) => (HandleCall(s.ElementAt(0)))
            );
        }

        private WoopsaValue HandleCall(IWoopsaValue requestsArgument)
        {
            var serializer = new JavaScriptSerializer();
            Request[] requestsList = serializer.Deserialize<Request[]>(requestsArgument.AsText);
            List<MultipleRequestResponse> responses = new List<MultipleRequestResponse>();
            foreach (Request request in requestsList)
            {
                string result = null;
                try
                {
                    if (request.Verb.Equals(WoopsaFormat.VerbRead))
                        result = _server.ReadValue(request.Path);
                    else if (request.Verb.Equals(WoopsaFormat.VerbMeta))
                        result = _server.GetMetadata(request.Path);
                    else if (request.Verb.Equals(WoopsaFormat.VerbWrite))
                        result = _server.WriteValue(request.Path, request.Value);
                    else if (request.Verb.Equals(WoopsaFormat.VerbInvoke))
                        result = _server.InvokeMethod(request.Path, 
                            request.Arguments.ToNameValueCollection());
                }
                catch (Exception e)
                {
                    result = WoopsaFormat.Serialize(e);
                }
                MultipleRequestResponse response = new MultipleRequestResponse();
                response.Id = request.Id;
                response.Result = result;
                responses.Add(response);
            }
            return WoopsaValue.CreateUnchecked(responses.Serialize(), WoopsaValueType.JsonData);
        }

        private WoopsaServer _server;
    }

    public class MultipleRequestResponse
    {
        public int Id { get; set; }
        public string Result { get; set; }
    }

    public class Request
    {
        public int Id { get; set; }

        public string Verb { get; set; }

        public string Path { get; set; }

        public string Value { get; set; }

        public Dictionary<string, string> Arguments { get; set; }

    }
}
