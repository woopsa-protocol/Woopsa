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
        /// returns the executing woopsa server in which we are executing, if any.
        /// return null if the current context is not a Woopsa request.
        /// </summary>
        public static WoopsaServer CurrentWoopsaServer { get { return _currentWoopsaServer; } }

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
            _subscriptionService.CanReconnectSubscriptionToNewObject += OnCanWatch;
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
        /// This event occurs when an exception is caught.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> HandledException;

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

        public event EventHandler<WoopsaLogEventArgs> Log;

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

        protected virtual void OnCanWatch(object sender, CanWatchEventArgs e)
        {
        }

        protected virtual void OnLog(WoopsaVerb verb, string path, WoopsaValue[] arguments,
            string result, bool isSuccess)
        {
            Log?.Invoke(this, new WoopsaLogEventArgs(verb, path, arguments, result, isSuccess));
        }

        protected virtual void OnHandledException(Exception e)
        {
            if (HandledException != null)
                HandledException(this, new ExceptionEventArgs(e));
        }

        [ThreadStatic]
        private static int _woopsaModelAccessCounter;

        protected internal void ExecuteBeforeWoopsaModelAccess()
        {
            _woopsaModelAccessCounter++;
            if (_woopsaModelAccessCounter == 1)
                OnBeforeWoopsaModelAccess();
        }

        protected internal void ExecuteAfterWoopsaModelAccess()
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
                _currentWoopsaServer = this;
                try
                {
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
                finally
                {
                    _currentWoopsaServer = null;
                }
            }
            catch (WoopsaNotFoundException e)
            {
                response.WriteError(HTTPStatusCode.NotFound, e.GetFullMessage(), e.Serialize(), MIMETypes.Application.JSON);
                OnHandledException(e);
            }
            catch (WoopsaInvalidOperationException e)
            {
                response.WriteError(HTTPStatusCode.BadRequest, e.GetFullMessage(), e.Serialize(), MIMETypes.Application.JSON);
                OnHandledException(e);
            }
            catch (WoopsaException e)
            {
                response.WriteError(HTTPStatusCode.InternalServerError, e.GetFullMessage(), e.Serialize(), MIMETypes.Application.JSON);
                OnHandledException(e);
            }
            catch (Exception e)
            {
                response.WriteError(HTTPStatusCode.InternalServerError, e.GetFullMessage(), e.Serialize(), MIMETypes.Application.JSON);
                OnHandledException(e);
            }
        }

        #region Core Functions
        internal string ReadValue(string path)
        {
            IWoopsaElement item = FindByPath(path);
            if ((item is IWoopsaProperty))
            {
                IWoopsaProperty property = item as IWoopsaProperty;
                string result = property.Value.Serialize();
                OnLog(WoopsaVerb.Read, path, NoArguments, result, true);
                return result;
            }
            else
            {
                string message = String.Format("Cannot read value of a non-WoopsaProperty for path {0}", path);
                OnLog(WoopsaVerb.Read, path, NoArguments, message, false);
                throw new WoopsaInvalidOperationException(message);
            }
        }

        private string WriteValue(string path, Func<WoopsaValueType, WoopsaValue> getValue)
        {
            IWoopsaElement item = FindByPath(path);
            if ((item is IWoopsaProperty))
            {
                IWoopsaProperty property = item as IWoopsaProperty;
                WoopsaValue argument = getValue(property.Type);
                if (!property.IsReadOnly)
                {
                    property.Value = argument;
                    string result = property.Value.Serialize();
                    OnLog(WoopsaVerb.Write, path, new WoopsaValue[] { argument }, result, true);
                    return result;
                }
                else
                {
                    string message = String.Format(
                        "Cannot write a read-only WoopsaProperty for path {0}", path);
                    OnLog(WoopsaVerb.Write, path, new WoopsaValue[] { argument }, message, false);
                    throw new WoopsaInvalidOperationException(message);
                }
            }
            else
            {
                WoopsaValue argument = getValue(WoopsaValueType.Text);
                string message = String.Format("Cannot write value of a non-WoopsaProperty for path {0}", path);
                OnLog(WoopsaVerb.Write, path, new WoopsaValue[] { argument }, message, false);
                throw new WoopsaInvalidOperationException(message);
            }
        }

        internal string WriteValue(string path, string value)
        {
            return WriteValue(path, (woopsaValueType) => WoopsaValue.CreateUnchecked(value, woopsaValueType));
        }

        internal string WriteValueDeserializedJson(string path, object deserializedJson)
        {
            return WriteValue(path, (woopsaValueType) => WoopsaValue.DeserializedJsonToWoopsaValue(deserializedJson, woopsaValueType));
        }

        static WoopsaValue[] NoArguments = new WoopsaValue[0];

        internal string GetMetadata(string path)
        {
            IWoopsaElement item = FindByPath(path);
            if (item is IWoopsaContainer)
            {
                string result;
                if (item is IWoopsaObject)
                    result = (item as IWoopsaObject).SerializeMetadata();
                else
                    result = (item as IWoopsaContainer).SerializeMetadata();
                OnLog(WoopsaVerb.Meta, path, NoArguments, result, true);
                return result;
            }
            else
            {
                string message = String.Format("Cannot get metadata for a WoopsaElement of type {0}", item.GetType());
                OnLog(WoopsaVerb.Meta, path, NoArguments, message, false);
                throw new WoopsaInvalidOperationException(message);
            }
        }

        private string InvokeMethod(string path, int argumentsCount,
            Func<string, WoopsaValueType, WoopsaValue> getArgumentByName)
        {
            IWoopsaElement item = FindByPath(path);
            if (item is IWoopsaMethod)
            {
                IWoopsaMethod method = item as IWoopsaMethod;
                if (argumentsCount == method.ArgumentInfos.Count())
                {
                    List<WoopsaValue> woopsaArguments = new List<WoopsaValue>();
                    foreach (var argInfo in method.ArgumentInfos)
                    {
                        WoopsaValue argumentValue = getArgumentByName(argInfo.Name, argInfo.Type);
                        if (argumentValue == null)
                        {
                            string message = String.Format("Missing argument {0} for method {1}", argInfo.Name, item.Name);
                            OnLog(WoopsaVerb.Invoke, path, NoArguments, message, false);
                            throw new WoopsaInvalidOperationException(message);
                        }
                        else
                            woopsaArguments.Add(argumentValue);
                    }
                    WoopsaValue[] argumentsArray = woopsaArguments.ToArray();
                    IWoopsaValue methodResult = method.Invoke(argumentsArray);
                    string result = methodResult != null ? methodResult.Serialize() : WoopsaConst.WoopsaNull;
                    OnLog(WoopsaVerb.Invoke, path, argumentsArray, result, true);
                    return result;
                }
                else
                {
                    string message = String.Format("Wrong argument count for method {0}", item.Name);
                    OnLog(WoopsaVerb.Invoke, path, NoArguments, message, false);
                    throw new WoopsaInvalidOperationException(message);
                }
            }
            else
            {
                string message = String.Format("Cannot invoke a {0}", item.GetType());
                OnLog(WoopsaVerb.Invoke, path, NoArguments, message, false);
                throw new WoopsaInvalidOperationException(message);
            }
        }

        internal string InvokeMethodDeserializedJson(string path, Dictionary<string, object> arguments)
        {
            int argumentsCount = arguments != null ? arguments.Count : 0;
            return InvokeMethod(path, argumentsCount,
                (argumentName, woopsaValueType) =>
                {
                    object argumentValue = arguments[argumentName];
                    if (argumentValue != null)
                        return WoopsaValue.DeserializedJsonToWoopsaValue(
                            argumentValue, woopsaValueType);
                    else
                        return null;
                });
        }

        internal string InvokeMethod(string path, NameValueCollection arguments)
        {
            int argumentsCount = arguments != null ? arguments.Count : 0;
            return InvokeMethod(path, argumentsCount,
                (argumentName, woopsaValueType) =>
                {
                    string argumentValue = arguments[argumentName];
                    if (argumentValue != null)
                        return WoopsaValue.CreateChecked(argumentValue, woopsaValueType);
                    else
                        return null;
                });
        }
        #endregion

        private IWoopsaElement FindByPath(string searchPath)
        {
            return _root.ByPath(searchPath);
        }
     
        #endregion

        [ThreadStatic]
        private static WoopsaServer _currentWoopsaServer;

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
        public bool Process(HTTPRequest request, HTTPResponse response)
        {
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

    public class WoopsaLogEventArgs : EventArgs
    {
        public WoopsaLogEventArgs(WoopsaVerb verb, string path, WoopsaValue[] arguments,
            string result, bool isSuccess)
        {
            Verb = verb;
            Path = path;
            Arguments = arguments;
            Result = result;
            IsSuccess = isSuccess;
        }

        public WoopsaVerb Verb { get; private set; }

        public string Path { get; private set; }

        public WoopsaValue[] Arguments { get; private set; }

        /// <summary>
        /// Result is valid only when IsSuccess is true. Otherwise, it contains the error message.
        /// </summary>
        public string Result { get; private set; }

        public bool IsSuccess { get; private set; }

    }
}
