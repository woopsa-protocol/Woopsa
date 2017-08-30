using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Woopsa
{
    /// <summary>
    /// Provides a mechanism to match a URL with an appropriate response.
    /// <para>
    /// The RouteSolver is a solution to the common problem of serving web pages based
    /// on the request made by a browser. It's an internal part of the WebServer
    /// and thus cannot be created from outside.
    /// </para>
    /// <para>
    /// The public methods, however, allow the user to add routes to the solver using
    /// either delegates for simple cases, or classes that implement the 
    /// <see cref="WebServer.IHTTPRouteHandler"/> interface.
    /// </para>
    /// </summary>
    public class RouteSolver
    {
        #region ctor
        internal RouteSolver()
        {
            _routes = new List<RouteMapper>();
        }
        #endregion

        #region Public Members

        #endregion

        #region Private/Protected/Internal Members
        private List<RouteMapper> _routes;
        internal event EventHandler<RoutingErrorEventArgs> Error;
        private object _lock = new object();
        #endregion

        #region public Methods
        /// <summary>
        /// Adds a route handler for the specified route and HTTP methods, using a delegate.
        /// <para>
        /// This is useful for quick, one-off serving of pages. For more complex cases, such as
        /// serving files from the file system, it is recommended to use the slightly more complex
        /// <see cref="IHTTPRouteHandler"/> interface.
        /// </para>
        /// </summary>
        /// <param name="route">The route, for example "/hello_world"</param>
        /// <param name="methods">
        /// The HTTP Methods to support. HTTPMethod has the <c>[Flag]</c> metadata,
        /// which makes it possible to support multiple methods in a single page.
        /// </param>
        /// <param name="handlerMethod">A method delegate. See <see cref="HandleRequestDelegate"/> for usage.</param>
        public RouteMapper Add(string route, HTTPMethod methods, HandleRequestDelegate handlerMethod, bool acceptSubroutes = false)
        {
            lock (_lock)
            {
                RouteHandlerDelegate newHandler = new RouteHandlerDelegate(handlerMethod, acceptSubroutes);
                RouteMapper mapper = new RouteMapper(route, methods, newHandler);
                _routes.Add(mapper);
                return mapper;
            }
        }

        /// <summary>
        /// Adds a route handler for the specified route and HTTP methods, using a class that
        /// implements the <see cref="IHTTPRouteHandler"/> delegate.
        /// </summary>
        /// <param name="route">The route, for example "/hello_world"</param>
        /// <param name="methods">
        /// The HTTP Methods to support. HTTPMethod has the <c>[Flag]</c> metadata,
        /// which makes it possible to support multiple methods in a single page.
        /// </param>
        /// <param name="handler">An object which implements the <see cref="IHTTPRouteHandler"/> interface</param>
        public RouteMapper Add(string route, HTTPMethod methods, IHTTPRouteHandler handler)
        {
            lock (_lock)
            {
                RouteMapper mapper = new RouteMapper(route, methods, handler);
                _routes.Add(mapper);
                return mapper;
            }
        }
        public bool Remove(RouteMapper routeMapper)
        {
            lock (_lock)
            {
                return _routes.Remove(routeMapper);
            }
        }
        public bool Remove(string route)
        {
            lock (_lock)
            {
                foreach (var item in _routes)
                {
                    if (item.Route.Equals(route))
                    {
                        _routes.Remove(item);
                        return true;
                    }
                }
            }
            return false;
        }

        public IEnumerable<RouteMapper> RouteMappers
        {
            get
            {
                foreach (RouteMapper route in _routes)
                    yield return route;
            }
        }
        #endregion

        #region Private/Protected/Internal Methods
        internal void HandleRequest(HTTPRequest request, HTTPResponse response, Stream stream)
        {
            try
            {
                bool matchFound = false;
                RouteMapper mapper = null;
                int i = 0;

                for (;;)
                {
                    lock (_routes)
                    {
                        if (i < _routes.Count)
                            mapper = _routes[i++];
                        else
                            break;
                    }
                    string regex = "^" + mapper.Route;
                    if (!mapper.AcceptSubroutes)
                    {
                        regex += "$";
                    }
                    if (Regex.IsMatch(request.BaseURL, regex))
                    {
                        if ((mapper.Methods & request.Method) != 0)
                        {
                            if (mapper.AcceptSubroutes)
                            {
                                int pos = request.BaseURL.IndexOf(mapper.Route);
                                request.Subroute = request.BaseURL.Substring(0, pos) + request.BaseURL.Substring(pos + mapper.Route.Length);
                            }
                            matchFound = true;
                            break;
                        }
                    }
                }

                if (!matchFound)
                {
                    response.WriteError(HTTPStatusCode.NotFound, "Not Found");
                    OnError(RoutingErrorType.NO_MATCHES, "No route found for request", request);
                }
                else
                {
                    mapper.HandleRequest(request, response);
                }
            }
            catch (Exception e)
            {
                response.WriteError(HTTPStatusCode.InternalServerError, String.Format("Internal Server Error {0}", e.Message));
                OnError(RoutingErrorType.INTERNAL, "A RouteHandler threw an exception.", request);
            }
        }

        protected virtual void OnError(RoutingErrorType errorType, string message, HTTPRequest request)
        {
            if (Error != null)
                Error(this, new RoutingErrorEventArgs(errorType, message, request));
        }
        #endregion
    }
}
