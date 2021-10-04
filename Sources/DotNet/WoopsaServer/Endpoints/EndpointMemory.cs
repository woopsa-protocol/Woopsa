
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Woopsa
{
    public class EndpointMemory : Endpoint
    {
        #region Constructor

        public EndpointMemory(string routePrefix, HTTPMethod httpMethod)
            : base(routePrefix)
        {
            _dictionary = new Dictionary<string, MemoryStream>();
            _httpMethod = httpMethod;
        }

        #endregion

        #region Public Methods

        public override async Task HandleRequestAsync(HttpContext context, string subRoute)
        {
            if ((_httpMethod & (HTTPMethod)Enum.Parse(typeof(HTTPMethod), context.Request.Method)) == 0)
            {
                await context.Response.WriteErrorAsync(HttpStatusCode.NotFound, "Not Found");
                return;
            }
            if (string.IsNullOrEmpty(subRoute))
            {
                //If there really is no file requested, then we send a 404!
                await context.Response.WriteErrorAsync(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            lock (_dictionary)
            {
                MemoryStream resource = null;
                if (_dictionary.TryGetValue(subRoute, out resource))
                {
                    resource.Position = 0;
                    context.Response.SetHeader("Cache-Control", "no-cache, no-store, must-revalidate");
                    context.Response.SetHeader("Pragma", "no-cache");
                    context.Response.SetHeader("Expires", "0");
                    _ = resource.CopyToAsync(context.Response.Body);

                }
                else
                    _ = context.Response.WriteErrorAsync(HttpStatusCode.NotFound, "Not Found");
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
                _dictionary[key] = memory;
        }

        public void DeleteResource(string key)
        {
            lock (_dictionary)
                _dictionary.Remove(key);
        }

        #endregion

        #region Private Methods

        private IDictionary<string, MemoryStream> _dictionary;
        private HTTPMethod _httpMethod;

        #endregion
    }
}
