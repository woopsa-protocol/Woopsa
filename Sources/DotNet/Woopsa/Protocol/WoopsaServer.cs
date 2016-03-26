using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Specialized;
using System.Globalization;
using System.Threading;

namespace Woopsa
{
    public enum WoopsaVerb
    {
        Read,
        Write,
        Meta,
        Invoke
    }

    public class EventArgsCachePath : EventArgs
    {
        public string Path { get; set; }
        public bool KeepInCache { get; set; }
    }

    public delegate bool CachePathDelegate(object sender, EventArgsCachePath args);

    public class WoopsaServer : IDisposable
    {
        public const string DefaultServerPrefix = "/woopsa/";
        public const int DefaultPort = 80;
        public const int DefaultPortSsl = 443;        
        public const ThreadPriority DefaultThreadPriority = ThreadPriority.Normal;
        public const bool DefaultKeepPathInCache = true;
        public const string WoopsaAuthenticationRealm = "Woopsa authentication";

        public WebServer WebServer { get; private set; }

        public string RoutePrefix { get { return _routePrefix; } }

        public bool AllowCrossOrigin { get; set; }

        public event CachePathDelegate PathCaching;

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
            AddRoutes();
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
        /// <param name="threadPoolSize">
        /// The maximum number of threads to be created. 
        /// CustomThreadPool.DefaultThreadPoolSize means use default operating system value.</param>
        /// <param name="priority">
        /// The priority of the server threads.
        /// </param>
        public WoopsaServer(IWoopsaContainer root, int port = DefaultPort, string routePrefix = DefaultServerPrefix,
            int threadPoolSize = CustomThreadPool.DefaultThreadPoolSize, ThreadPriority priority = DefaultThreadPriority)
            : this(root, new WebServer(port, threadPoolSize, priority), routePrefix)
        {
            _isWebServerEmbedded = true;
            WebServer.Start();
        }

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
        public WoopsaServer(object root, int port = DefaultPort, string routePrefix = DefaultServerPrefix) :
            this(new WoopsaObjectAdapter(null, root.GetType().Name, root), port, routePrefix)
        {
            new WoopsaMultiRequestHandler((WoopsaObject)_root, this);
            new SubscriptionService((WoopsaObject)_root);
        }

        public void ClearCache()
        {
            _pathCache = new Dictionary<string, IWoopsaElement>();
            // TODO : design a more general approach for clearing the root or sub nodes
            if (_root is WoopsaObjectAdapter)
                (_root as WoopsaObjectAdapter).ClearCache();
        }

        protected virtual bool OnCachingPath(string path)
        {
            EventArgsCachePath args = new EventArgsCachePath();
            args.Path = path;
            args.KeepInCache = DefaultKeepPathInCache; 
            if (PathCaching != null)
                PathCaching(this, args);
            return args.KeepInCache;
        }

        /// <summary>
        /// The authenticator in use by Woopsa. Set to null if no authenticator is used. 
        /// </summary>
        public BaseAuthenticator Authenticator
        {
            get
            {
                return _authenticator;
            }
            set
            {
                if (_authenticator != null)
                {
                    _prefixRouteMapper.RemoveProcessor(_authenticator);
                    _metaRouteMapper.AddProcessor(_authenticator);
                    _readRouteMapper.AddProcessor(_authenticator);
                    _writeRouteMapper.AddProcessor(_authenticator);
                    _invokeRouteMapper.AddProcessor(_authenticator);
                    _authenticator = null;
                }
                if (value != null)
                {
                    _prefixRouteMapper.AddProcessor(_authenticator);
                    _metaRouteMapper.AddProcessor(_authenticator);
                    _readRouteMapper.AddProcessor(_authenticator);
                    _writeRouteMapper.AddProcessor(_authenticator);
                    _invokeRouteMapper.AddProcessor(_authenticator);
                    _authenticator = value;
                }
            }
        }
        
        private BaseAuthenticator _authenticator;

        #region Private Members
        private void AddRoutes()
        {
            AllowCrossOrigin = true;
            _prefixRouteMapper = WebServer.Routes.Add(_routePrefix, HTTPMethod.OPTIONS, (Request, response) => { }, true);
            _prefixRouteMapper.AddProcessor(_accessControlProcessor);
            // meta route
            _metaRouteMapper = WebServer.Routes.Add(_routePrefix + "meta", HTTPMethod.GET, 
                (request, response) => { HandleRequest(WoopsaVerb.Meta, request, response); }, true);
            _metaRouteMapper.AddProcessor(_accessControlProcessor);
            // read route
            _readRouteMapper = WebServer.Routes.Add(_routePrefix + "read", HTTPMethod.GET,
                (request, response) => { HandleRequest(WoopsaVerb.Read, request, response); }, true);
            _readRouteMapper.AddProcessor(_accessControlProcessor);
            // write route
            _writeRouteMapper = WebServer.Routes.Add(_routePrefix + "write", HTTPMethod.POST, 
                (request, response) => { HandleRequest(WoopsaVerb.Write, request, response); }, true);
            _writeRouteMapper.AddProcessor(_accessControlProcessor);
            // POST is used here instead of GET for two main reasons:
            //  - The length of a GET query is limited in HTTP. There is no official limit but most
            //    implementations have a 2-8 KB limit, which is not good when we want to do large
            //    multi-requestsList, for example
            //  - GET requestsList should not change the state of the server, as they can be triggered
            //    by crawlers and such. Invoking a function will, in most cases, change the state of
            //    the server.
            _invokeRouteMapper = WebServer.Routes.Add(_routePrefix + "invoke", HTTPMethod.POST,
                (request, response) => { HandleRequest(WoopsaVerb.Invoke, request, response); }, true);
            _invokeRouteMapper.AddProcessor(_accessControlProcessor);
        }

        private void RemoveRoutes()
        {
            WebServer.Routes.Remove(_prefixRouteMapper);
            WebServer.Routes.Remove(_metaRouteMapper);
            WebServer.Routes.Remove(_readRouteMapper);
            WebServer.Routes.Remove(_writeRouteMapper);
            WebServer.Routes.Remove(_invokeRouteMapper);
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
                response.WriteError(HTTPStatusCode.NotFound, e.Message, WoopsaFormat.WoopsaError(e), MIMETypes.Application.JSON);
            }
            catch (WoopsaInvalidOperationException e)
            {
                response.WriteError(HTTPStatusCode.BadRequest, e.Message, WoopsaFormat.WoopsaError(e), MIMETypes.Application.JSON);
            }
            catch (WoopsaException e)
            {
                response.WriteError(HTTPStatusCode.InternalServerError, e.Message, WoopsaFormat.WoopsaError(e), MIMETypes.Application.JSON);
            }
            catch (Exception e)
            {
                response.WriteError(HTTPStatusCode.InternalServerError, e.Message, WoopsaFormat.WoopsaError(e), MIMETypes.Application.JSON);
            }
        }

        #region Core Functions
        internal string ReadValue(string path)
        {
            IWoopsaElement item = FindByPath(path);
            if ((item is IWoopsaProperty))
            {
                IWoopsaProperty property = item as IWoopsaProperty;
                return property.Value.Serialize();
            }
            else
                throw new WoopsaInvalidOperationException(String.Format("Cannot read value of a non-WoopsaProperty for path {0}", path));
        }

        internal string WriteValue(string path, string value)
        {
            IWoopsaElement item = FindByPath(path);
            if ((item is IWoopsaProperty))
            {
                IWoopsaProperty property = item as IWoopsaProperty;
                if (property.IsReadOnly)
                {
                    throw new WoopsaInvalidOperationException(String.Format("Cannot write a read-only WoopsaProperty for path {0}", path));
                }
                property.Value = WoopsaValue.CreateUnchecked(value, property.Type);
                return property.Value.Serialize();
            }
            else
            {
                throw new WoopsaInvalidOperationException(String.Format("Cannot read value of a non-WoopsaProperty for path {0}", path));
            }
        }
        internal string GetMetadata(string path)
        {
            IWoopsaElement item = FindByPath(path);
            if (item is IWoopsaObject)
                return (item as IWoopsaObject).SerializeMetadata();
            else if (item is IWoopsaContainer)
                return (item as IWoopsaContainer).SerializeMetadata();
            else
                throw new WoopsaInvalidOperationException(String.Format("Cannot get metadata for a WoopsaElement of type {0}", item.GetType()));
        }
        internal string InvokeMethod(string path, NameValueCollection arguments)
        {
            if (arguments == null)
                arguments = new NameValueCollection();

            IWoopsaElement item = FindByPath(path);
            if (item is IWoopsaMethod)
            {
                IWoopsaMethod method = item as IWoopsaMethod;
                List<WoopsaValue> wArguments = new List<WoopsaValue>();

                if (arguments.Count != method.ArgumentInfos.Count())
                    throw new WoopsaInvalidOperationException(String.Format("Wrong argument count for method {0}", item.Name));
                foreach (var argInfo in method.ArgumentInfos)
                {
                    string argumentValue = arguments[argInfo.Name];
                    if (argumentValue == null)
                        throw new WoopsaInvalidOperationException(String.Format("Missing argument {0} for method {1}", argInfo.Name, item.Name));
                    wArguments.Add(WoopsaValue.CreateUnchecked(argumentValue, argInfo.Type));
                }
                IWoopsaValue result = method.Invoke(wArguments);
                return (result != null) ? result.Serialize() : string.Empty;
            }
            else
                throw new WoopsaInvalidOperationException(String.Format("Cannot invoke a {0}", item.GetType()));
        }
        #endregion

        private IWoopsaContainer _root;
        private string _routePrefix;
        private bool _isWebServerEmbedded = false;
        private Dictionary<string, IWoopsaElement> _pathCache = new Dictionary<string, IWoopsaElement>();
        private AccessControlProcessor _accessControlProcessor = new AccessControlProcessor();

        private RouteMapper _prefixRouteMapper, _readRouteMapper, _writeRouteMapper, 
            _metaRouteMapper, _invokeRouteMapper;

        private IWoopsaElement FindByPath(string path)
        {
            if (path.Equals(WoopsaConst.WoopsaPathSeparator.ToString())) //This is the root object
                return _root;

            path = path.TrimStart(WoopsaConst.WoopsaPathSeparator);
            IWoopsaElement item = _root;

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
                        item = _pathCache[currentPath];
                    }
                    else
                    {
                        if (item is IWoopsaObject)
                            item = (item as IWoopsaObject).ByName(toFind);
                        else if (item is IWoopsaContainer)
                            item = (item as IWoopsaContainer).Items.ByName(toFind);

                        if (OnCachingPath(currentPath))
                            _pathCache.Add(currentPath, item);
                    }
                    pathAt++;
                }
                while (pathAt < pathParts.Length);
            }
            else
                item = _pathCache[path];

            return item;
        }
        #endregion

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                RemoveRoutes();
                if (_isWebServerEmbedded)
                    WebServer.Dispose();
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
                response.SetHeader("Access-Control-Max-Age", MaxAge.TotalSeconds.ToString(CultureInfo.InvariantCulture));
                return true;
            }
        }
    }
}
