using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Woopsa
{
    public class WoopsaClientMultiRequest
    {
        public WoopsaClientMultiRequest()
        {
            _clientRequests = new List<Woopsa.WoopsaClientRequest>();
            _clientRequestsById = new Dictionary<int, Woopsa.WoopsaClientRequest>();
        }

        public IEnumerable<WoopsaClientRequest> ClientRequests { get { return _clientRequests; } }

        public IEnumerable<ClientRequest> Requests
        {
            get
            {
                foreach (var item in _clientRequests)
                    yield return item.Request;
            }
        }

        public IEnumerable<WoopsaClientRequestResult> ClientRequestResults
        {
            get
            {
                foreach (var item in _clientRequests)
                    yield return item.Result;
            }
        }

        public WoopsaClientRequest Meta(string objectPath)
        {
            WoopsaClientRequest newRequest = new WoopsaClientRequest()
            {
                Request = new ClientRequest()
                {
                    Id = GetNextRequestId(),
                    Verb = WoopsaFormat.VerbMeta,
                    Path = objectPath,
                }
            };
            Add(newRequest);
            return newRequest;
        }

        public WoopsaClientRequest Invoke(string methodPath,
            WoopsaMethodArgumentInfo[] argumentInfos, params WoopsaValue[] arguments)
        {
            if (argumentInfos.Length == arguments.Length)
            {
                Dictionary<string, WoopsaValue> dictionary = new Dictionary<string, WoopsaValue>();
                for (int i = 0; i < argumentInfos.Length; i++)
                    dictionary[argumentInfos[i].Name] = arguments[i];
                return Invoke(methodPath, dictionary);
            }
            else
                throw new Exception(string.Format(
                    "{0} argumentInfos do not match with {1} arguments",
                    argumentInfos.Length, arguments.Length));
        }

        public WoopsaClientRequest Invoke(string methodPath, Dictionary<string, WoopsaValue> arguments)
        {
            WoopsaClientRequest newRequest = new WoopsaClientRequest()
            {
                Request = new ClientRequest()
                {
                    Id = GetNextRequestId(),
                    Verb = WoopsaFormat.VerbInvoke,
                    Path = methodPath,
                    Arguments = arguments
                }
            };
            Add(newRequest);
            return newRequest;
        }

        public WoopsaClientRequest Read(string propertyPath)
        {
            WoopsaClientRequest newRequest = new WoopsaClientRequest()
            {
                Request = new ClientRequest()
                {
                    Id = GetNextRequestId(),
                    Verb = WoopsaFormat.VerbRead,
                    Path = propertyPath
                }
            };
            Add(newRequest);
            return newRequest;
        }

        public WoopsaClientRequest Write(string propertyPath, WoopsaValue value)
        {
            WoopsaClientRequest newRequest = new WoopsaClientRequest()
            {
                Request = new ClientRequest()
                {
                    Id = GetNextRequestId(),
                    Verb = WoopsaFormat.VerbWrite,
                    Path = propertyPath,
                    Value = value
                }
            };
            Add(newRequest);
            return newRequest;
        }

        /// <summary>
        /// Removes all the requests 
        /// </summary>
        public void Clear()
        {
            _clientRequests.Clear();
            _clientRequestsById.Clear();
        }

        public int Count { get { return _clientRequests.Count; } }

        public WoopsaClientRequest RequestById(int Id)
        {
            WoopsaClientRequest result;
            _clientRequestsById.TryGetValue(Id, out result);
            return result;
        }

        /// <summary>
        /// Clear the flag IsDone on all the requests
        /// Useful before re-executing a multirequest
        /// </summary>
        public void Reset()
        {
            foreach (var item in _clientRequests)
            {
                item.IsDone = false;
                item.Result = null;
            }
        }

        private int GetNextRequestId()
        {
            return _clientRequests.Count + 1;
        }

        private void Add(WoopsaClientRequest clientRequest)
        {
            _clientRequestsById[clientRequest.Request.Id] = clientRequest;
            _clientRequests.Add(clientRequest);
        }
        internal void DispatchResults(WoopsaJsonData jsonData)
        {
            if (jsonData.IsArray)
                for (int i = 0; i < jsonData.Length; i++)
                {
                    WoopsaJsonData item = jsonData[i];
                    int id = item[WoopsaFormat.KeyId];
                    WoopsaClientRequest request;
                    if (_clientRequestsById.TryGetValue(id, out request))
                    {
                        WoopsaJsonData result = item[WoopsaFormat.KeyResult];
                        if (result.ContainsKey(WoopsaFormat.KeyError))
                        {
                            request.Result = new WoopsaClientRequestResult()
                            {
                                Error = WoopsaFormat.DeserializeError(result.AsText),
                                ResultType = WoopsaClientRequestResultType.Error
                            };
                        }
                        else if (request.Request.Verb == WoopsaFormat.VerbMeta)
                        {
                            request.Result = new WoopsaClientRequestResult()
                            {
                                Meta = WoopsaFormat.DeserializeMeta(result.AsText),
                                ResultType = WoopsaClientRequestResultType.Meta
                            };
                        }
                        else
                        {
                            request.Result = new WoopsaClientRequestResult()
                            {
                                Value = WoopsaFormat.DeserializeWoopsaValue(result.AsText),
                                ResultType = WoopsaClientRequestResultType.Value
                            };
                        }
                        request.IsDone = true;
                    }
                    else
                        throw new WoopsaException(
                            string.Format("MultiRequest received a result for an unkwnon request Id={0}", id));
                }
            else
                throw new WoopsaException("MultiRequest response has invalid format");
        }

        private List<WoopsaClientRequest> _clientRequests;
        private Dictionary<int, WoopsaClientRequest> _clientRequestsById;
    }

    public class WoopsaClientRequest
    {
        public ClientRequest Request { get; internal set; }

        public bool IsDone { get; internal set; }

        public WoopsaClientRequestResult Result { get; internal set; }

    }

    public enum WoopsaClientRequestResultType { Value, Error, Meta }

    public class WoopsaClientRequestResult
    {
        public WoopsaClientRequestResultType ResultType { get; internal set; }

        public Exception Error { get; internal set; }

        public WoopsaValue Value { get; internal set; }

        public WoopsaMetaResult Meta { get; internal set; }
    }

    public class ClientRequest
    {
        public int Id { get; set; }

        public string Verb { get; set; }

        public string Path { get; set; }

        public WoopsaValue Value { get; set; }

        public Dictionary<string, WoopsaValue> Arguments { get; set; }

    }
}
