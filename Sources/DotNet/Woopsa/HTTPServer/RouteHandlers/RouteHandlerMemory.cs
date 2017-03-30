
using System.Collections.Generic;
using System.IO;

namespace Woopsa
{
    public class RouteHandlerMemory : IHTTPRouteHandler
    {
        public RouteHandlerMemory()
        {
            _dictionary = new Dictionary<string, MemoryStream>();
        }

        public void HandleRequest(HTTPRequest request, HTTPResponse response)
        {
            if (request.Subroute == "")
            {
                //If there really is no file requested, then we send a 404!
                response.WriteError(HTTPStatusCode.NotFound, "Not Found");
                return;
            }

            lock (_dictionary)
            {
                MemoryStream resource = null;
                if (_dictionary.TryGetValue(request.Subroute, out resource))
                {
                    resource.Position = 0;
                    response.WriteStream(resource);
                    response.SetHeader("Cache-Control", "no-cache, no-store, must-revalidate");
                    response.SetHeader("Pragma", "no-cache");
                    response.SetHeader("Expires", "0");
                }
                else
                    response.WriteError(HTTPStatusCode.NotFound, "Not Found");
            }
        }

        public void RegisterResource(string key, MemoryStream memory)
        {
            lock (_dictionary)
                _dictionary.Add(key, memory);
        }

        public void UpdateResource(string key, MemoryStream memory)
        {
            lock (_dictionary)
                _dictionary[key] =  memory;
        }

        public void DeleteResource(string key)
        {
            lock (_dictionary)
                _dictionary.Remove(key);
        }

        public bool AcceptSubroutes
        {
            get { return true; }
        }

        private IDictionary<string, MemoryStream> _dictionary;
    }
}
