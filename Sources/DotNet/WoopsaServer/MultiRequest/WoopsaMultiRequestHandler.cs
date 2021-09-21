using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Woopsa
{
    public class WoopsaMultiRequestHandler
    {
        #region Constructor

        public WoopsaMultiRequestHandler(WoopsaObject root, EndpointWoopsa woopsaEndPoint)
        {
            _woopsaEndPoint = woopsaEndPoint;

            new WoopsaMethod(root,
                WoopsaMultiRequestConst.WoopsaMultiRequestMethodName,
                WoopsaValueType.JsonData,
                new List<WoopsaMethodArgumentInfo> { new WoopsaMethodArgumentInfo(WoopsaMultiRequestConst.WoopsaMultiRequestArgumentName, WoopsaValueType.JsonData) },
                (s) => (HandleCall(s.ElementAt(0)))
            );
        }

        #endregion

        #region Fields / Attributes

        private EndpointWoopsa _woopsaEndPoint;

        #endregion

        #region Private Methods

        private WoopsaValue HandleCall(IWoopsaValue requestsArgument)
        {
            using (new WoopsaServerModelAccessFreeSection(_woopsaEndPoint))
            {
                ServerRequest[] requestsList = JsonSerializer.Deserialize<ServerRequest[]>(requestsArgument.AsText, WoopsaUtils.ObjectToInferredTypesConverterOptions);
                List<MultipleRequestResponse> responses = new List<MultipleRequestResponse>();
                foreach (var request in requestsList)
                {
                    string result = null;
                    try
                    {
                        using (new WoopsaServerModelAccessLockedSection(_woopsaEndPoint))
                        {
                            if (request.Verb.Equals(WoopsaFormat.VerbRead))
                                result = _woopsaEndPoint.ReadValue(request.Path);
                            else if (request.Verb.Equals(WoopsaFormat.VerbMeta))
                                result = _woopsaEndPoint.GetMetadata(request.Path);
                            else if (request.Verb.Equals(WoopsaFormat.VerbWrite))
                                result = _woopsaEndPoint.WriteValueDeserializedJson(request.Path, request.Value);
                            else if (request.Verb.Equals(WoopsaFormat.VerbInvoke))
                                result = _woopsaEndPoint.InvokeMethodDeserializedJson(request.Path,
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

        #endregion
    }
}
