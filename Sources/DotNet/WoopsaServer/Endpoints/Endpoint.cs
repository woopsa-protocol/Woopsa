using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Woopsa
{
    public abstract class Endpoint
    {
        #region Constructor

        protected Endpoint(string routePrefix)
        {
            RoutePrefix = WoopsaUtils.RemoveInitialAndFinalSeparator(routePrefix); ;
        }

        #endregion

        #region Properties

        public string RoutePrefix { get; }

        public BaseAuthenticator Authenticator { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// This event occurs when an exception is caught.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> HandledException;

        public event EventHandler<WoopsaLogEventArgs> Log;

        internal event EventHandler<RoutingErrorEventArgs> Error;

        #endregion

        #region Public Methods

        public bool SetCurrentWebServer(int port, Action setCurrentWebServer)
        {
            return _currentWebServerSetter.TryAdd(port, setCurrentWebServer);
        }

        public IEndpointConventionBuilder MapEndPoint(IEndpointRouteBuilder endpoints)
        {
            const string SubRoute = "subRoute";
            return endpoints.Map($"{RoutePrefix}/{{**{SubRoute}}}",
                async (context) =>
                {
                    int port = context.Request.Host.Port.HasValue ? context.Request.Host.Port.Value : 80;
                    if (_currentWebServerSetter.ContainsKey(port))
                    {
                        _currentWebServerSetter[port].Invoke();
                    }
                    try
                    {
                        string subRoute = context.Request.RouteValues[SubRoute]?.ToString();
                       
                        if (!await CheckAuthentificationAsync(context.Request, context.Response))
                        {
                            return;
                        }
                        else
                        {
                            await HandleRequestAsync(context, subRoute);
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                });
        }

        public abstract Task HandleRequestAsync(HttpContext context, string subRoute);

        #endregion

        #region Protected / Private Methods

        private async Task<bool> CheckAuthentificationAsync(HttpRequest request, HttpResponse response)
        {
            if (Authenticator is not null)
            {
                bool authenticated = Authenticator.IsAuthenticated(request, response);
                if (!authenticated)
                {
                    response.Headers.Add(HTTPHeader.WWWAuthenticate, string.Format(HTTPHeader.BasicAuthResponseHeaderValue, Authenticator.Realm));
                    response.Headers[HTTPHeader.ContentType] = MIMETypes.Text.HTML;
                    await response.WriteErrorAsync(HttpStatusCode.Unauthorized, nameof(HttpStatusCode.Unauthorized));
                    return false;
                }
            }
            return true;
        }

        protected virtual void OnLog(WoopsaVerb verb, string path, WoopsaValue[] arguments,
            string result, bool isSuccess)
        {
            Log?.Invoke(this, new WoopsaLogEventArgs(verb, path, arguments, result, isSuccess));
        }

        protected virtual void OnHandledException(Exception e)
        {
            HandledException?.Invoke(this, new ExceptionEventArgs(e));
        }

        protected virtual void OnError(RoutingErrorType errorType, string message, HttpRequest request)
        {
            Error?.Invoke(this, new RoutingErrorEventArgs(errorType, message, request));
        }

        #endregion

        #region Fields / Attributes

        private ConcurrentDictionary<int, Action> _currentWebServerSetter = new ConcurrentDictionary<int, Action>();

        #endregion
    }
}
