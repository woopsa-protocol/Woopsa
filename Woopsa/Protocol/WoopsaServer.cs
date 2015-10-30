using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Diagnostics;
using Woopsa;

namespace Woopsa
{
    public enum WoopsaVerb
    {
        Read,
        Write,
        Meta,
        Invoke
    }

	public class WoopsaServer : IDisposable
    {
        public const string DefaultServerPrefix = "/woopsa/";
        public const int DefaultPort = 80;
        public const int DefaultPortSsl = 443;

        public WebServer WebServer { get; private set; }

        public string RoutePrefix { get { return _routePrefix; } }

        public bool AllowCrossOrigin { get; set; }

        /// <summary>
        /// Creates an instance of the Woopsa server with a new Reflector for the object 
        /// passed to it. 
        /// 
        /// It will automatically create the required HTTP server
        /// and all necessary native extensions to enable Publish/Subscribe and 
        /// Multi-Requests.
        /// </summary>
        /// <param name="root">The root object that will be published via Woopsa.</param>
        public WoopsaServer(object root) 
            : this(root, DefaultPort, DefaultServerPrefix) { }

        /// <summary>
        /// Creates an instance of the Woopsa server with a new Reflector for the object 
        /// passed to it. 
        /// 
        /// It will automatically create the required HTTP server
        /// on the specified port and all necessary native extensions for Publish/
        /// Subscribe and Mutli-Requests.
        /// </summary>
        /// <param name="root">The root object that will be published via Woopsa.</param>
        /// <param name="port">The port on which to run the web server</param>
        public WoopsaServer(object root, int port) 
            : this(root, port, DefaultServerPrefix) { }

        /// <summary>
        /// Creates an instance of the Woopsa server with a new Reflector for the object 
        /// passed to it. 
        /// 
        /// It will automatically create the required HTTP server
        /// on the specified port and will prefix woopsa verbs with the specified 
        /// route prefix. It will also add all the necessary native extensions for 
        /// Publish/Subscribe and Mutli-Requests.
        /// </summary>
        /// <param name="root">The root object that will be published via Woopsa.</param>
        /// <param name="port">The port on which to run the web server</param>
        /// <param name="routePrefix">
        /// The prefix to add to all routes for woopsa verbs. For example, specifying
        /// "myPrefix" will make the server available on http://server/myPrefix
        /// </param>
        public WoopsaServer(object root, int port, string routePrefix)
        {
            WoopsaObjectAdapter adapter = new WoopsaObjectAdapter(null, root.GetType().Name, root);
            _root = adapter;
            _selfCreatedServer = true;
            _routePrefix = routePrefix;
            WebServer = new WebServer(port, true);
            AddRoutes(WebServer, routePrefix);
            new WoopsaMultiRequestHandler(adapter, this);
            new SubscriptionService(adapter);
            WebServer.Start();
        }

        /// <summary>
        /// Creates an instance of the Woopsa server without using the Reflector. You
        /// will have to create the object hierarchy yourself, using WoopsaObjects 
        /// or implementing IWoopsaContainer yourself.
        /// 
        /// It will automatically create the required HTTP server
        /// and all necessary native extensions to enable Publish/Subscribe and 
        /// Multi-Requests.
        /// </summary>
        /// <param name="root">The root object that will be published via Woopsa.</param>
        public WoopsaServer(IWoopsaContainer root) 
            : this(root, new WebServer(80, true)) 
        {
            _selfCreatedServer = true;
            WebServer.Start();
        }

        /// <summary>
        /// Creates an instance of the Woopsa server without using the Reflector. You
        /// will have to create the object hierarchy yourself, using WoopsaObjects 
        /// or implementing IWoopsaContainer yourself.
        /// 
        /// It will automatically create the required HTTP server
        /// on the specified port and all necessary native extensions for Publish/
        /// Subscribe and Mutli-Requests.
        /// </summary>
        /// <param name="root">The root object that will be published via Woopsa.</param>
        /// <param name="port">The port on which to run the web server</param>
        public WoopsaServer(IWoopsaContainer root, int port)
            : this(root, new WebServer(port, true))
        {
            _selfCreatedServer = true;
            WebServer.Start();
        }

        /// <summary>
        /// Creates an instance of the Woopsa server without using the Reflector. You
        /// will have to create the object hierarchy yourself, using WoopsaObjects 
        /// or implementing IWoopsaContainer yourself.
        /// 
        /// It will automatically create the required HTTP server
        /// on the specified port and will prefix woopsa verbs with the specified 
        /// route prefix. It will also add all the necessary native extensions for 
        /// Publish/Subscribe and Mutli-Requests.
        /// </summary>
        /// <param name="root">The root object that will be published via Woopsa.</param>
        /// <param name="port">The port on which to run the web server</param>
        /// <param name="routePrefix">
        /// The prefix to add to all routes for woopsa verbs. For example, specifying
        /// "myPrefix" will make the server available on http://server/myPrefix
        /// </param>
        public WoopsaServer(IWoopsaContainer root, int port, string routePrefix)
            : this(root, new WebServer(port, true), routePrefix)
        {
            _selfCreatedServer = true;
            WebServer.Start();
        }

        /// <summary>
        /// Creates an instance of the Woopsa server without using the Reflector. You
        /// will have to create the object hierarchy yourself, using WoopsaObjects 
        /// or implementing IWoopsaContainer yourself.
        /// 
        /// If you are already using a <see cref="WebServer"/> somewhere else,
        /// this constructor will simply add the woopsa routes to that server.
        /// This allows you to serve static content and the Woopsa protocol
        /// on the same network interface and on the same port.
        /// </summary>
        /// <param name="root">The root object that will be published via Woopsa.</param>
        /// <param name="server">The server on which Woopsa routes will be added</param>
        /// <param name="routePrefix">
        /// The prefix to add to all routes for woopsa verbs. For example, specifying
        /// "myPrefix" will make the server available on http://server/myPrefix
        /// </param>
        public WoopsaServer(IWoopsaContainer root, WebServer server, string routePrefix = DefaultServerPrefix)
        {
            _root = root;
            WebServer = server;
            _routePrefix = routePrefix;
            AddRoutes(server, routePrefix);
        }

        public void ClearCache()
        {
            _pathCache = new Dictionary<string, IWoopsaElement>();
            if (_root is WoopsaObjectAdapter)
                (_root as WoopsaObjectAdapter).ClearCache();
        }

        public WWWAuthenticator.Check CheckAuthenticate 
        {
            get
            {
                return _checkAuthenticate;
            }
            set
            {
                _checkAuthenticate = value;
                if (_checkAuthenticate == null)
                {
                    foreach (RouteMapper mapper in WebServer.Routes.RouteMappers)
                        mapper.RemoveProcessor(_authenticator);
                    _authenticator = null;
                }
                else if (_authenticator == null)
                {
                    _authenticator = new WWWAuthenticator("Woopsa authentication");
                    _authenticator.DoCheck = _checkAuthenticate;
                    _metaRoute.AddProcessor(_authenticator);
                    _readRoute.AddProcessor(_authenticator);
                    _writeRoute.AddProcessor(_authenticator);
                    _invokeRoute.AddProcessor(_authenticator);
                }
                _authenticator.DoCheck = _checkAuthenticate;
            }
        }
        private WWWAuthenticator.Check _checkAuthenticate;
        private WWWAuthenticator _authenticator;

        #region Private Members
        private void AddRoutes(WebServer server, string routePrefix)
        {
            AllowCrossOrigin = true;
            server.Routes.Add(routePrefix, HTTPMethod.OPTIONS, (Request, response) => { }, true).AddProcessor(_accessControlProcessor);
            _metaRoute = server.Routes.Add(routePrefix + "meta", HTTPMethod.GET, (request, response) => { HandleRequest(WoopsaVerb.Meta, request, response); }, true).AddProcessor(_accessControlProcessor);
            _readRoute = server.Routes.Add(routePrefix + "read", HTTPMethod.GET, (request, response) => { HandleRequest(WoopsaVerb.Read, request, response); }, true).AddProcessor(_accessControlProcessor);
            _writeRoute = server.Routes.Add(routePrefix + "write", HTTPMethod.POST, (request, response) => { HandleRequest(WoopsaVerb.Write, request, response); }, true).AddProcessor(_accessControlProcessor);
            // POST is used here instead of GET for two main reasons:
            //  - The length of a GET query is limited in HTTP. There is no official limit but most
            //    implementations have a 2-8 KB limit, which is not good when we want to do large
            //    multi-requestsList, for example
            //  - GET requestsList should not change the state of the server, as they can be triggered
            //    by crawlers and such. Invoking a function will, in most cases, change the state of
            //    the server.
            _invokeRoute = server.Routes.Add(routePrefix + "invoke", HTTPMethod.POST, (request, response) => { HandleRequest(WoopsaVerb.Invoke, request, response); }, true).AddProcessor(_accessControlProcessor);
        }

        private void HandleRequest(WoopsaVerb verb, HTTPRequest request, HTTPResponse response)
        {
            try
            {
                // This is the first thing we do, that way even 404 errors have the right headers
                if (AllowCrossOrigin)
                    response.SetHeader("Access-Control-Allow-Origin", "*");
                string result = null;
                switch (verb)
                {
                    case WoopsaVerb.Meta:
                        result = GetMetadata(request.Subroute);
                        break;
                    case WoopsaVerb.Read:
                        result = ReadValue(request.Subroute);
                        break;
                    case WoopsaVerb.Write:
                        result = WriteValue(request.Subroute, request.Body["value"]);
                        break;
                    case WoopsaVerb.Invoke:
                        result = InvokeMethod(request.Subroute, request.Body);
                        break;
                }
                response.SetHeader(HTTPHeader.ContentType, MIMETypes.Application.JSON);
                response.WriteString(result);
            }
            catch (WoopsaNotFoundException e)
            {
                response.WriteError(HTTPStatusCode.NotFound, e.Message, WoopsaFormat.WoopsaError(e.Message), MIMETypes.Application.JSON);
            }
            catch (WoopsaInvalidOperationException e)
            {
                response.WriteError(HTTPStatusCode.BadRequest, e.Message, WoopsaFormat.WoopsaError(e.Message), MIMETypes.Application.JSON);
            }
            catch (Exception e)
            {
                response.WriteError(HTTPStatusCode.InternalServerError, e.Message, WoopsaFormat.WoopsaError(e.Message), MIMETypes.Application.JSON);
            }
        }

        #region Core Functions
        internal string ReadValue(string path)
        {
            IWoopsaElement elem = FindByPath(path);
            if ((elem is IWoopsaProperty))
            {
                IWoopsaProperty property = elem as IWoopsaProperty;
                string value = property.Value.AsText;
                return property.Value.Serialise();
            }
            else
                throw new WoopsaInvalidOperationException(String.Format("Cannot read value of a non-WoopsaProperty for path {0}", path));
        }

        internal string WriteValue(string path, string value)
        {
            IWoopsaElement elem = FindByPath(path);
			if ((elem is IWoopsaProperty))
            {
                IWoopsaProperty property = elem as IWoopsaProperty;
                if (property.IsReadOnly)
                {
                    throw new WoopsaInvalidOperationException(String.Format("Cannot write a read-only WoopsaProperty for path {0}", path));
                }
                property.Value = new WoopsaValue(value, property.Type);
                return property.Value.Serialise();
            }
            else
            {
                throw new WoopsaInvalidOperationException(String.Format("Cannot read value of a non-WoopsaProperty for path {0}", path));
            }
        }

        internal string GetMetadata(string path)
        {
            IWoopsaElement elem = FindByPath(path);
            if (elem is IWoopsaObject)
            {
                return (elem as IWoopsaObject).SerializeMetadata();
            }
            else if (elem is IWoopsaContainer)
            {
                return (elem as IWoopsaContainer).SerializeMetadata();
            }
            else
            {
                throw new WoopsaInvalidOperationException(String.Format("Cannot get metadata for a WoopsaElement of type {0}", elem.GetType()));
            }
        }

        internal string InvokeMethod(string path, NameValueCollection arguments)
        {
            if (arguments == null)
                arguments = new NameValueCollection();

            IWoopsaElement elem = FindByPath(path);
            if (elem is IWoopsaMethod)
            {
                IWoopsaMethod method = elem as IWoopsaMethod;
                List<WoopsaValue> wArguments = new List<WoopsaValue>();

                if (arguments.Count != method.ArgumentInfos.Count())
                {
                    throw new WoopsaInvalidOperationException(String.Format("Wrong argument count for method {0}", elem.Name));
                }

                foreach (var argInfo in method.ArgumentInfos)
                {
                    string argumentValue = arguments[argInfo.Name];
                    if (argumentValue == null)
                        throw new WoopsaInvalidOperationException(String.Format("Missing argument {0} for method {1}", argInfo.Name, elem.Name));
                    wArguments.Add(new WoopsaValue(argumentValue, argInfo.Type));
                }

                IWoopsaValue result = method.Invoke(wArguments);

                if (result != null)
                {
                    return result.Serialise();
                }
                else
                {
                    return "";
                }
            }
            else
            {
                throw new WoopsaInvalidOperationException(String.Format("Cannot invoke a {0}", elem.GetType()));
            }
        }
        #endregion

        private IWoopsaContainer _root;
        private string _routePrefix;
        private bool _selfCreatedServer = false;
        private Dictionary<string, IWoopsaElement> _pathCache = new Dictionary<string, IWoopsaElement>();
        private AccessControlProcessor _accessControlProcessor = new AccessControlProcessor();

        private RouteMapper _readRoute;
        private RouteMapper _writeRoute;
        private RouteMapper _metaRoute;
        private RouteMapper _invokeRoute;

        private void Stop()
        {
            WebServer.Routes.Remove(_routePrefix + "meta");
            WebServer.Routes.Remove(_routePrefix + "read");
            WebServer.Routes.Remove(_routePrefix + "write");
            WebServer.Routes.Remove(_routePrefix + "invoke");
            if (_selfCreatedServer)
                WebServer.Stop();
        }

        private IWoopsaElement FindByPath(string path)
        {
            if (path.Equals(WoopsaConst.WoopsaPathSeparator.ToString())) //This is the root object
                return _root;

            path = path.TrimStart(WoopsaConst.WoopsaPathSeparator);
            IWoopsaElement elem = _root;

            if (!_pathCache.ContainsKey(path))
            {
                string[] pathParts = path.Split(WoopsaConst.WoopsaPathSeparator);
                int pathAt = 0;

                do
                {
                    string toFind = pathParts.Skip(pathAt).ElementAt(0);
                    string currentPath = String.Join(WoopsaConst.WoopsaPathSeparator.ToString(), pathParts.Take(pathAt + 1));

                    if (_pathCache.ContainsKey(currentPath))
                    {
                        elem = _pathCache[currentPath];
                    }
                    else
                    {
                        if (elem is IWoopsaObject)
                            elem = (elem as IWoopsaObject).ByName(toFind);
                        else if (elem is IWoopsaContainer)
                            elem = (elem as IWoopsaContainer).Items.ByName(toFind);

                        _pathCache.Add(currentPath, elem);
                    }
                    pathAt++;
                }
                while (pathAt < pathParts.Length);
            }
            else
                elem = _pathCache[path];

            return elem;
        }
        #endregion

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        private class AccessControlProcessor : PostRouteProcessor, IRequestProcessor
        {
            public static readonly TimeSpan MaxAge = TimeSpan.FromDays(20);

            public bool Process(HTTPRequest request, HTTPResponse response)
            {
                response.SetHeader("Access-Control-Allow-Headers", "Authorization");
                response.SetHeader("Access-Control-Allow-Origin", "*");
                response.SetHeader("Access-Control-Allow-Credentials", "true");
                response.SetHeader("Access-Control-Max-Age", MaxAge.TotalSeconds.ToString());
                return true;
            }
        }
    }
}
