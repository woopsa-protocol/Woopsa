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

    public class WoopsaServer : IDisposable
    {
        public const string DefaultServerPrefix = "/woopsa/";
        public const int DefaultPort = 80;
        public const int DefaultPortSsl = 443;
        public const ThreadPriority DefaultThreadPriority = ThreadPriority.Normal;
        public const string WoopsaAuthenticationRealm = "Woopsa authentication";

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
        /// Creates an instance of the Woopsa server adding multiRequestHandler and SubscriptionService
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
        public WoopsaServer(WoopsaObject root, int port = DefaultPort, string routePrefix = DefaultServerPrefix) :
            this((IWoopsaContainer)root, port, routePrefix)
        {
            new WoopsaMultiRequestHandler(root, this);
            _subscriptionService = new WoopsaSubscriptionService(this, root);
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
        }

        public WebServer WebServer { get; private set; }

        public string RoutePrefix { get { return _routePrefix; } }

        public bool AllowCrossOrigin { get; set; }

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
                    _metaRouteMapper.RemoveProcessor(_authenticator);
                    _readRouteMapper.RemoveProcessor(_authenticator);
                    _writeRouteMapper.RemoveProcessor(_authenticator);
                    _invokeRouteMapper.RemoveProcessor(_authenticator);
                    _authenticator = null;
                }
                if (value != null)
                {
                    _prefixRouteMapper.AddProcessor(value);
                    _metaRouteMapper.AddProcessor(value);
                    _readRouteMapper.AddProcessor(value);
                    _writeRouteMapper.AddProcessor(value);
                    _invokeRouteMapper.AddProcessor(value);
                    _authenticator = value;
                }
            }
        }

        /// <summary>
        /// This event is triggered before WoopsaServer accesses the WoopsaObject model.
        /// It is usefull to protect against concurrent WoopsaObject accesses at a global level.
        /// </summary>
        public event EventHandler BeforeWoopsaModelAccess;

        /// <summary>
        /// This event is triggered after WoopsaServer accesses the WoopsaObject model.
        /// It is guaranteed that for each BeforeWoopsaModelAccess event fired, 
        /// the AfterWoopsaModelAccess event will be fired.
        /// </summary>
        public event EventHandler AfterWoopsaModelAccess;

        /// <summary>
        /// This method simply calls BeforeWoopsaModelAccess to trigger concurrent access 
        /// protection over the woopsa model.
        /// </summary>
        /// <returns>
        /// a WoopsaServerModelAccessFreeSection that must be disposed to leave the
        /// Model access-free section
        /// </returns>
        public WoopsaServerModelAccessFreeSection EnterModelAccessFreeSection()
        {
            return new WoopsaServerModelAccessFreeSection(this);
        }

        public void ShutDown()
        {
            if (_subscriptionService != null)
                _subscriptionService.Terminate();
            WebServer.Shutdown();
        }

        protected virtual void OnBeforeWoopsaModelAccess()
        {
            if (BeforeWoopsaModelAccess != null)
                BeforeWoopsaModelAccess(this, new EventArgs());
        }

        protected virtual void OnAfterWoopsaModelAccess()
        {
            if (AfterWoopsaModelAccess != null)
                AfterWoopsaModelAccess(this, new EventArgs());
        }

        [ThreadStatic]
        private static int _woopsaModelAccessCounter;

        internal protected void ExecuteBeforeWoopsaModelAccess()
        {
            _woopsaModelAccessCounter++;
            if (_woopsaModelAccessCounter == 1)
                OnBeforeWoopsaModelAccess();
        }

        internal protected void ExecuteAfterWoopsaModelAccess()
        {
            _woopsaModelAccessCounter--;
            if (_woopsaModelAccessCounter == 0)
                OnAfterWoopsaModelAccess();
            else if (_woopsaModelAccessCounter < 0)
                throw new WoopsaException("Woopsa internal model lock counting error");
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ShutDown();
                RemoveRoutes();
                if (_subscriptionService != null)
                {
                    _subscriptionService.Dispose();
                    _subscriptionService = null;
                }
                if (_isWebServerEmbedded)
                {
                    WebServer.Dispose();
                    WebServer = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Private Members
        private void AddRoutes()
        {
            AllowCrossOrigin = true;
            _prefixRouteMapper = WebServer.Routes.Add(_routePrefix, HTTPMethod.OPTIONS, (Request, response) => { }, true);
            _prefixRouteMapper.AddProcessor(_accessControlProcessor);
            // meta route
            _metaRouteMapper = WebServer.Routes.Add(_routePrefix + WoopsaFormat.VerbMeta, HTTPMethod.GET,
                (request, response) => { HandleRequest(WoopsaVerb.Meta, request, response); }, true);
            _metaRouteMapper.AddProcessor(_accessControlProcessor);
            // read route
            _readRouteMapper = WebServer.Routes.Add(_routePrefix + WoopsaFormat.VerbRead, HTTPMethod.GET,
                (request, response) => { HandleRequest(WoopsaVerb.Read, request, response); }, true);
            _readRouteMapper.AddProcessor(_accessControlProcessor);
            // write route
            _writeRouteMapper = WebServer.Routes.Add(_routePrefix + WoopsaFormat.VerbWrite, HTTPMethod.POST,
                (request, response) => { HandleRequest(WoopsaVerb.Write, request, response); }, true);
            _writeRouteMapper.AddProcessor(_accessControlProcessor);
            // POST is used here instead of GET for two main reasons:
            //  - The length of a GET query is limited in HTTP. There is no official limit but most
            //    implementations have a 2-8 KB limit, which is not good when we want to do large
            //    multi-requestsList, for example
            //  - GET requestsList should not change the state of the server, as they can be triggered
            //    by crawlers and such. Invoking a function will, in most cases, change the state of
            //    the server.
            _invokeRouteMapper = WebServer.Routes.Add(_routePrefix + WoopsaFormat.VerbInvoke, HTTPMethod.POST,
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
                    // TODO: constantes symboliques
                    response.SetHeader("Access-Control-Allow-Origin", "*");
                string result = null;
                ExecuteBeforeWoopsaModelAccess();
                try
                {
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
                }
                finally
                {
                    ExecuteAfterWoopsaModelAccess();
                }
                response.SetHeader(HTTPHeader.ContentType, MIMETypes.Application.JSON);
                if (result != null)
                    response.WriteString(result);
            }
            catch (WoopsaNotFoundException e)
            {
                response.WriteError(HTTPStatusCode.NotFound, e.Message, WoopsaFormat.Serialize(e), MIMETypes.Application.JSON);
            }
            catch (WoopsaInvalidOperationException e)
            {
                response.WriteError(HTTPStatusCode.BadRequest, e.Message, WoopsaFormat.Serialize(e), MIMETypes.Application.JSON);
            }
            catch (WoopsaException e)
            {
                response.WriteError(HTTPStatusCode.InternalServerError, e.Message, WoopsaFormat.Serialize(e), MIMETypes.Application.JSON);
            }
            catch (Exception e)
            {
                response.WriteError(HTTPStatusCode.InternalServerError, e.Message, WoopsaFormat.Serialize(e), MIMETypes.Application.JSON);
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
                List<WoopsaValue> woopsaArguments = new List<WoopsaValue>();

                if (arguments.Count != method.ArgumentInfos.Count())
                    throw new WoopsaInvalidOperationException(String.Format("Wrong argument count for method {0}", item.Name));
                foreach (var argInfo in method.ArgumentInfos)
                {
                    string argumentValue = arguments[argInfo.Name];
                    if (argumentValue == null)
                        throw new WoopsaInvalidOperationException(String.Format("Missing argument {0} for method {1}", argInfo.Name, item.Name));
                    woopsaArguments.Add(WoopsaValue.CreateChecked(argumentValue, argInfo.Type));
                }
                IWoopsaValue result = method.Invoke(woopsaArguments.ToArray());
                return result != null ? result.Serialize() : WoopsaConst.WoopsaNull;
            }
            else
                throw new WoopsaInvalidOperationException(String.Format("Cannot invoke a {0}", item.GetType()));
        }
        #endregion

        private IWoopsaElement FindByPath(string searchPath)
        {
            return _root.ByPath(searchPath);
        }
        #endregion

        private BaseAuthenticator _authenticator;
        private WoopsaSubscriptionService _subscriptionService;
        private IWoopsaContainer _root;
        private string _routePrefix;
        private bool _isWebServerEmbedded = false;
        private AccessControlProcessor _accessControlProcessor = new AccessControlProcessor();

        private RouteMapper _prefixRouteMapper, _readRouteMapper, _writeRouteMapper,
            _metaRouteMapper, _invokeRouteMapper;

    }
    internal class AccessControlProcessor : PostRouteProcessor, IRequestProcessor
    {
        public static readonly TimeSpan MaxAge = TimeSpan.FromDays(20);

        public bool Process(HTTPRequest request, HTTPResponse response)
        {
            response.SetHeader("Access-Control-Allow-Headers", "Authorization");
            // TODO : constante symbolique + déterminer si AllowCrossOrigin doit être utilisé ici
            response.SetHeader("Access-Control-Allow-Origin", "*");
            response.SetHeader("Access-Control-Allow-Credentials", "true");
            response.SetHeader("Access-Control-Max-Age", MaxAge.TotalSeconds.ToString(CultureInfo.InvariantCulture));
            // Make IE stop cacheing AJAX requests
            response.SetHeader("Cache-Control", "no-cache, no-store");
            return true;
        }
    }

    public sealed class WoopsaServerModelAccessFreeSection : IDisposable
    {
        internal WoopsaServerModelAccessFreeSection(WoopsaServer server)
        {
            _server = server;
            server.ExecuteAfterWoopsaModelAccess();
        }

        public void Dispose()
        {
            _server.ExecuteBeforeWoopsaModelAccess();
        }

        private WoopsaServer _server;
    }
}
