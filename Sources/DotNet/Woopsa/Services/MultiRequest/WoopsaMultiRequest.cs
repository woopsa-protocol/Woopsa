using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

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
            using (new WoopsaServerModelAccessFreeSection(_server))
            {
                ServerRequest[] requestsList = JsonSerializer.Deserialize<ServerRequest[]>(requestsArgument.AsText, WoopsaUtils.ObjectToInferredTypesConverterOptions);
                List<MultipleRequestResponse> responses = new List<MultipleRequestResponse>();
                foreach (var request in requestsList)
                {
                    string result = null;
                    try
                    {
                        using (new WoopsaServerModelAccessLockedSection(_server))
                        {
                            if (request.Verb.Equals(WoopsaFormat.VerbRead))
                                result = _server.ReadValue(request.Path);
                            else if (request.Verb.Equals(WoopsaFormat.VerbMeta))
                                result = _server.GetMetadata(request.Path);
                            else if (request.Verb.Equals(WoopsaFormat.VerbWrite))
                                result = _server.WriteValueDeserializedJson(request.Path, request.Value);
                            else if (request.Verb.Equals(WoopsaFormat.VerbInvoke))
                                result = _server.InvokeMethodDeserializedJson(request.Path,
                                    request.Arguments);
                        }
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
        }

        private WoopsaServer _server;
    }

    public class MultipleRequestResponse
    {
        public int Id { get; set; }
        public string Result { get; set; }
    }

    public class ServerRequest
    {
        public int Id { get; set; }

        public string Verb { get; set; }

        public string Path { get; set; }

        public object Value { get; set; }

        public Dictionary<string, object> Arguments { get; set; }

    }
}
