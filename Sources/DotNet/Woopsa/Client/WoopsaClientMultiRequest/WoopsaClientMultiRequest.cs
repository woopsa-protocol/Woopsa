using System;
using System.Collections.Generic;

namespace Woopsa
{
    public class WoopsaClientMultiRequest
    {
        #region Constructor

        public WoopsaClientMultiRequest()
        {
            _clientRequests = new List<Woopsa.WoopsaClientRequest>();
            _clientRequestsById = new Dictionary<int, Woopsa.WoopsaClientRequest>();
        }

        #endregion

        #region Fields / Attributes

        public IEnumerable<WoopsaClientRequest> ClientRequests => _clientRequests;

        public int Count => _clientRequests.Count;

        private int GetNextRequestId() => _clientRequests.Count + 1;

        private List<WoopsaClientRequest> _clientRequests;
        private Dictionary<int, WoopsaClientRequest> _clientRequestsById;

        #endregion

        #region Properties

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

        #endregion

        #region Public Methods

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

        public WoopsaClientRequest Invoke(string methodPath, WoopsaMethodArgumentInfo[] argumentInfos, params WoopsaValue[] arguments)
        {
            if (argumentInfos.Length == arguments.Length)
            {
                Dictionary<string, WoopsaValue> dictionary = new Dictionary<string, WoopsaValue>();
                for (int i = 0; i < argumentInfos.Length; i++)
                    dictionary[argumentInfos[i].Name] = arguments[i];
                return Invoke(methodPath, dictionary);
            }
            else
            {
                string message = string.Format("{0} argumentInfos do not match with {0} arguments", argumentInfos.Length);
                throw new Exception(message);
            }
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

        public WoopsaClientRequest RequestById(int Id)
        {
            _clientRequestsById.TryGetValue(Id, out WoopsaClientRequest result);
            return result;
        }

        /// <summary>
        /// Removes all the requests 
        /// </summary>
        public void Clear()
        {
            _clientRequests.Clear();
            _clientRequestsById.Clear();
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

        #endregion

        #region Private Methods

        private void Add(WoopsaClientRequest clientRequest)
        {
            _clientRequestsById[clientRequest.Request.Id] = clientRequest;
            _clientRequests.Add(clientRequest);
        }

        #endregion

        #region Internal Methods

        internal void DispatchResults(WoopsaJsonData jsonData)
        {
            if (jsonData.IsArray)
                for (int i = 0; i < jsonData.Length; i++)
                {
                    WoopsaJsonData item = jsonData[i];
                    int id = item[WoopsaFormat.KeyId];

                    if (_clientRequestsById.TryGetValue(id, out WoopsaClientRequest request))
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
                    {
                        throw new WoopsaException(
                            string.Format("MultiRequest received a result for an unkwnon request Id={0}", id));
                    }
                }
            else
            {
                throw new WoopsaException("MultiRequest response has invalid format");
            }
        }

        #endregion
    }
}
