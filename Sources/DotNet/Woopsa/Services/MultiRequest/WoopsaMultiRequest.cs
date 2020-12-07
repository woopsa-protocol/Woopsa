using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Woopsa
{
    public static class WoopsaMultiRequestConst
    {
        public const string WoopsaMultiRequestMethodName = "MultiRequest";
        public const string WoopsaMultiRequestArgumentName = "Requests";
    }

    public class ObjectToInferredTypesConverter
         : JsonConverter<object>
    {
        public override object Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) => reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.Number when reader.TryGetInt64(out long l) => l,
                JsonTokenType.Number => reader.GetDouble(),
                JsonTokenType.String when reader.TryGetDateTime(out DateTime datetime) => datetime,
                JsonTokenType.String => reader.GetString(),
                _ => DefaultContent(ref reader)
            };
        private JsonElement DefaultContent(ref Utf8JsonReader reader)
        {
            using (var document = JsonDocument.ParseValue(ref reader))
                return document.RootElement.Clone();
        }
        public override void Write(
            Utf8JsonWriter writer,
            object objectToWrite,
            JsonSerializerOptions options) =>
            throw new InvalidOperationException("Should not get here.");
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
                var deserializeOptions = new JsonSerializerOptions
                {
                    Converters =
                    {
                        new ObjectToInferredTypesConverter()
                    }
                };
                ServerRequest[] requestsList = JsonSerializer.Deserialize<ServerRequest[]>(requestsArgument.AsText, deserializeOptions);
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
