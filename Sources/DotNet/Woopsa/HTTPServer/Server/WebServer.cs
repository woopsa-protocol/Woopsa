using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web;

namespace Woopsa
{
    // TODO : AllowCrossOrigin on error responses

    /// <summary>
    /// Provides a simple Web Server
    /// <para>
    /// A Web server can be single-threaded or multi-threaded. When it is single-threaded, only a single thread handles all incoming requests. This means that browsers will have to wait until the response is fully delivered before making the next request. This also means that the Connection: Keep-Alive HTTP mechanism cannot be used.
    /// </para>
    /// <para>
    /// If the server is multi-threaded, then a ThreadPool is used to handle client requests and the Connection: Keep-Alive mechanism is thus supported. This makes page loading times significantly faster, as the browser doesn't need to close and re-open a TCP connection for every resource it fetches.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This Web Server is not meant to replace complex servers such as Apache, NGinx or ISS. It does not support more complex mechanisms such as:
    /// <list type="bullet">
    /// <item>POST requests encoded with multipart/form-data (file uploads)</item>
    /// <item>HTTP Methods other than POST, GET, PUT, DELETE (must be handled manually)</item>
    /// </list>
    /// </remarks>
    public class WebServer : IDisposable
    {
        public const int DefaultPortHttp = 80;
        public const int DefaultPortHttps = 443;
        public const ThreadPriority DefaultThreadPriority = ThreadPriority.Normal;

        /// <summary>
        /// returns the current web server in which we are executing.
        /// return null if the current context is not a thread of the webserver.
        /// </summary>
        public static WebServer CurrentWebServer { get { return _currentWebServer; } }


        /// <summary>
        /// Creates a WebServer that runs on the specified port and can be multithreaded
        /// </summary>
        /// <param name="port">
        /// The port on which to run the server (default 80)
        /// </param>
        /// <param name="threadPoolSize">
        /// The maximum number of threads to be created. 
        /// CustomThreadPool.DefaultThreadPoolSize means use default operating system value.
        /// </param>
        /// <param name="priority">
        /// The priority of the web server threads.
        /// </param>
        /// <remarks>
        /// A server must be multithreaded in order to use the Keep-Alive HTTP mechanism.
        /// </remarks>
        public WebServer(int port = DefaultPortHttp, int threadPoolSize = CustomThreadPool.DefaultThreadPoolSize, ThreadPriority priority = DefaultThreadPriority)
        {
            Port = port;
            PreRouteProcessors = new List<PreRouteProcessor>();
            Routes = new RouteSolver();
            Routes.Error += _routeSolver_Error;
            _openTcpClients = new List<TcpClient>();
            _listener = new TcpListener(IPAddress.IPv6Any, port);
            _listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            if (threadPoolSize == CustomThreadPool.DefaultThreadPoolSize || threadPoolSize > 1)
                _threadPool = new CustomThreadPool("WoopsaWebServer", threadPoolSize, priority);
            _listenerThread = new Thread(Listen);
            _listenerThread.Priority = priority;
            _listenerThread.Name = "WebServer_Listener";
            HTTPResponse.Error += HTTPResponse_Error;
        }

        #region Public Members
        /// <summary>
        /// The RouteSolver allows a user to configure routes on the web server. This member is created internally and as such is read-only.
        /// </summary>
        public RouteSolver Routes { get; private set; }

        /// <summary>
        /// This event is raised whenever an error occurs inside the web server. In most cases, the error can be ignored, but odd behavior might occur when there are multiple matching routes, for example.
        /// </summary>
        public event EventHandler Error;

        public event EventHandler<LogEventArgs> Log;

        public event EventHandler<CorsRequestArgs> CorsRequest;

        /// <summary>
        /// Whether this server is using a thread pool to handle requests
        /// </summary>
        public bool MultiThreaded { get { return _threadPool != null; } }

        /// <summary>
        /// Which TCP port the WebServer is currently listening on
        /// </summary>
        public int Port { get; private set; }

        public bool Aborted { get { return _aborted; } }

        public List<PreRouteProcessor> PreRouteProcessors { get; private set; }
        #endregion

        #region Private Members
        private TcpListener _listener;
        private Thread _listenerThread;
        private List<TcpClient> _openTcpClients;
        private CustomThreadPool _threadPool;

        [ThreadStatic]
        private static WebServer _currentWebServer;

        private Dictionary<string, HTTPMethod> _supportedMethods = new Dictionary<string, HTTPMethod>()
        {
            {"GET",     HTTPMethod.GET},
            {"POST",    HTTPMethod.POST},
            {"PUT",     HTTPMethod.PUT},
            {"DELETE",  HTTPMethod.DELETE},
            {"OPTIONS", HTTPMethod.OPTIONS}
        };

        private bool _aborted = false;
        private bool _started = false;
        #endregion

        #region Public Methods
        /// <summary>
        /// Starts listening on the TCP socket and initiates the routing mechanism. At this point, the server become reachable by any client.
        /// </summary>
        public void Start()
        {
            _listener.Start();
            _started = true;
            _listenerThread.Start();
        }

        /// <summary>
        /// Shutdowns the server and stops listening for TCP connexions. At this point, the server becomes completely unreachable.
        /// It cannot be restarted.
        /// </summary>
        public void Shutdown()
        {
            if (!_aborted)
            {
                _listener.Stop();
                lock (_openTcpClients)
                    foreach (var item in _openTcpClients)
                        item.Close();
                _aborted = true;
                if (_threadPool != null)
                    _threadPool.Terminate();
            }
        }
        #endregion


        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Shutdown();
                _listenerThread.Join();
                _listener = null;
                if (_threadPool != null)
                {
                    _threadPool.Join();
                    _threadPool.Dispose();
                    _threadPool = null;
                }

            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion


        #region Private Methods
        private void Listen()
        {
            _currentWebServer = this;
            while (!_aborted && _started)
            {
                try
                {
                    // The HandleClient method will loop in case of
                    // Keep-Alive connections.
                    // The HandleClient method should NEVER close the stream
                    // as this is done from the upper scope.
                    TcpClient client = _listener.AcceptTcpClient();
                    client.NoDelay = true;
                    if (MultiThreaded)
                    {
                        try
                        {
                            _threadPool.StartUserWorkItem(
                                (o) =>
                                {
                                    try
                                    {
                                        _currentWebServer = this;
                                        try
                                        {
                                            HandleClient(client);
                                        }
                                        finally
                                        {
                                            client.Close();
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        OnError(new ThreadExceptionEventArgs(e));
                                    }
                                });
                        }
                        catch (Exception)
                        {
                            client.Close();
                            throw;
                        }
                    }
                    else
                    {
                        try
                        {
                            HandleClient(client);
                        }
                        finally
                        {
                            client.Close();
                        }
                    }
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.Interrupted)
                    {
                        _aborted = true;
                    }
                }
                catch (Exception)
                {
                    // TODO : mechanism to manage exceptions
                }
            }
            _listener.Stop();
        }

        private void HandleClient(TcpClient client)
        {
            lock (_openTcpClients)
                _openTcpClients.Add(client);
            try
            {
                Stream stream = client.GetStream();
                try
                {
                    foreach (PreRouteProcessor processor in PreRouteProcessors)
                    {
                        stream = processor.ProcessStream(stream);
                    }
                    bool leaveOpen = true;
                    HTTPResponse response = null;
                    HTTPRequest request = null;
                    StreamReader reader = new StreamReader(stream, Encoding.UTF8, false, 4096);
                    try
                    {
                        while (leaveOpen && !_aborted)
                        {
                            response = new HTTPResponse();
                            /*
                                * Parse the first line of the HTTP Request
                                * Examples:
                                *      GET / HTTP/1.1
                                *      POST /submit HTTP/1.1
                                */
                            string requestString;
                            try
                            {
                                requestString = reader.ReadLine();
                            }
                            catch (Exception e)
                            {
                                requestString = null;
                                leaveOpen = false;
                                break;
                            }
                            if (requestString == null)
                                break;

                            string[] parts = requestString.Split(' ');
                            if (parts.Length != 3)
                            {
                                throw new HandlingException(HTTPStatusCode.BadRequest, "Bad Request");
                            }
                            string method = parts[0].ToUpper();
                            string url = parts[1];
                            string version = parts[2].ToUpper();

                            //Check if the version is what we expect
                            if (version != "HTTP/1.1" && version != "HTTP/1.0")
                            {
                                throw new HandlingException(HTTPStatusCode.HttpVersionNotSupported, "HTTP Version Not Supported");
                            }

                            //Check if the method is supported
                            if (!_supportedMethods.ContainsKey(method))
                            {
                                throw new HandlingException(HTTPStatusCode.NotImplemented, method + " Method Not Implemented");
                            }
                            HTTPMethod httpMethod = _supportedMethods[method];

                            url = HttpUtility.UrlDecode(url);

                            //Build the request object
                            request = new HTTPRequest(httpMethod, url);

                            //Add all headers to the request object
                            FillHeaders(request, reader);

                            //Handle encoding for this request
                            Encoding clientEncoding = InferEncoding(request);

                            //Extract all the data from the URL (base and query)
                            ExtractQuery(request, clientEncoding);

                            //Extract and decode all the POST data
                            ExtractPOST(request, reader, clientEncoding);

                            bool keepAlive = false;
                            // According to spec, Keep-Alive is ON by default
                            if (version == "HTTP/1.1")
                                keepAlive = true;
                            if (request.Headers.ContainsKey(HTTPHeader.Connection))
                            {
                                if (request.Headers[HTTPHeader.Connection].ToLower().Equals("close"))
                                {
                                    keepAlive = false;
                                    response.SetHeader(HTTPHeader.Connection, "close");
                                }
                            }

                            //Keep-Alive can only work on a multithreaded server!
                            if (!keepAlive || !MultiThreaded)
                            {
                                leaveOpen = false;
                                response.SetHeader(HTTPHeader.Connection, "close");
                            }
                            //Pass this on to the route solver
                            OnLog(request, null);
                            HandleRequest(request, response, stream);
                            response.Respond(stream);
                            OnLog(request, response);
                        }
                    }
                    catch (HandlingException e)
                    {
                        if (response != null)
                        {
                            try
                            {
                                // try to return the response
                                response.WriteError(e.Status, e.ErrorMessage);
                                response.Respond(stream);
                                OnLog(request, response);
                            }
                            catch
                            {
                                // ignore silently if it is not posible
                            }
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        // Do nothing, server is terminating
                    }
                    catch (Exception e)
                    {
                        if (response != null)
                        {
                            try
                            {
                                // try to return the response
                                response.WriteError(HTTPStatusCode.InternalServerError, "Internal Server Error. " + e.Message);
                                response.Respond(stream);
                                OnLog(request, response);
                            }
                            catch
                            {
                                // ignore silently if it is not posible
                            }
                        }
                    }
                    finally
                    {
                        reader.Close();
                    }
                }
                finally
                {
                    stream.Dispose();
                }
            }
            finally
            {
                lock (_openTcpClients)
                    _openTcpClients.Remove(client);
            }
        }

        protected virtual void HandleRequest(HTTPRequest request, HTTPResponse response, Stream stream)
        {
            bool executeRequest = true;
            response.SetHeader("Access-Control-Allow-Credentials", "true");
            if (request.Headers.ContainsKey(HTTPHeader.Origin))
                executeRequest = HandleCorsRequest(request, response, stream);
            if (executeRequest)
                Routes.HandleRequest(request, response, stream);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="stream"></param>
        /// <returns>true if the request must be executed normally, false otherwise</returns>
        protected virtual bool HandleCorsRequest(HTTPRequest request, HTTPResponse response, Stream stream)
        {
            // 1. Prepare default values in event args
            CorsRequestArgs eventArgs = new Woopsa.CorsRequestArgs(request, response, stream);
            eventArgs.AccessControlMaxAgeValue = HTTPHeader.AccessControlMaxAgeDefaultValue;
            if (request.Headers.ContainsKey(HTTPHeader.AccessControlRequestHeaders))
                eventArgs.AccessControlAllowHeadersValue = request.Headers[HTTPHeader.AccessControlRequestHeaders];
            eventArgs.AccessControlAllowOriginValue = HTTPHeader.AccessControlAllowOriginDefaultValue;
            if (request.Headers.ContainsKey(HTTPHeader.AccessControlRequestMethod))
                eventArgs.AccessControlAllowMethodsValue = HTTPHeader.AccessControlAllowMethodsDefaultValue;
            eventArgs.IsPreflightCorsRequest = request.Method == HTTPMethod.OPTIONS;
            eventArgs.ExecuteRequest = !eventArgs.IsPreflightCorsRequest;
            // 2. call event
            if (CorsRequest != null)
                CorsRequest(this, eventArgs);
            // 3. Set headers into response
            // AccessControlAllowOrigin must be set for simple and preflight CORS requests
            if (eventArgs.AccessControlAllowOriginValue != null)
                response.SetHeader(HTTPHeader.AccessControlAllowOrigin,
                    eventArgs.AccessControlAllowOriginValue);
            if (eventArgs.IsPreflightCorsRequest)
            {
                // Set additional preflight CORS request headers 
                if (eventArgs.AccessControlAllowHeadersValue != null)
                    response.SetHeader(HTTPHeader.AccessControlAllowHeaders,
                        eventArgs.AccessControlAllowHeadersValue);
                if (eventArgs.AccessControlAllowMethodsValue != null)
                    response.SetHeader(HTTPHeader.AccessControlAllowMethods,
                        eventArgs.AccessControlAllowMethodsValue);
                if (eventArgs.AccessControlMaxAgeValue != null)
                    response.SetHeader(HTTPHeader.AccessControlMaxAge, eventArgs.AccessControlMaxAgeValue.Value.
                        TotalSeconds.ToString(CultureInfo.InvariantCulture));
            }
            return eventArgs.ExecuteRequest;
        }

        private void FillHeaders(HTTPRequest request, StreamReader reader)
        {
            string headerLine;
            while ((headerLine = reader.ReadLine()) != null && !headerLine.Equals(""))
            {
                if (headerLine.IndexOf(':') == -1)
                {
                    throw new HandlingException(HTTPStatusCode.BadRequest, "Bad Request");
                }
                string[] newHeader = headerLine.Split(':');
                request._headers.Add(newHeader[0].Replace(" ", "").ToLower(), newHeader[1].Trim());
            }
        }

        private Encoding InferEncoding(HTTPRequest request)
        {
            //According to the HTTP spec, ISO-8859-1 is the default encoding when dealing with the web.
            //However, UTF-8 has shown way more versatile and is thus used in most places.
            Encoding clientEncoding = Encoding.GetEncoding("ISO-8859-1");
            if (request.Headers.ContainsKey(HTTPHeader.ContentType))
            {
                string[] typeParts = request.Headers[HTTPHeader.ContentType].Split(';');
                string clientEncodingString = "ISO-8859-1";
                foreach (string typePart in typeParts)
                {
                    if (typePart.IndexOf("charset") != -1)
                    {
                        clientEncodingString = typePart.Split('=')[1].Replace(" ", "");
                        break;
                    }
                }
                clientEncoding = Encoding.GetEncoding(clientEncodingString);
            }
            return clientEncoding;
        }

        private void ExtractQuery(HTTPRequest request, Encoding clientEncoding)
        {
            string url = request.FullURL;
            string baseUrl, queryString;
            if (url.IndexOf('?') != -1)
            {
                NameValueCollection queryArguments;
                string[] queryParts = url.Split('?');
                baseUrl = queryParts[0];
                queryString = queryParts[1];
                queryArguments = HttpUtility.ParseQueryString(queryString, clientEncoding);
                request.Query = queryArguments;
            }
            else
            {
                baseUrl = Uri.UnescapeDataString(url);
                queryString = "";
            }
            request.BaseURL = baseUrl;
        }

        private void ExtractPOST(HTTPRequest request, StreamReader reader, Encoding clientEncoding)
        {
            if (request.Method == HTTPMethod.POST)
            {
                if (!request.Headers.ContainsKey(HTTPHeader.ContentLength))
                {
                    throw new HandlingException(HTTPStatusCode.LengthRequired, "Length Required");
                }
                int contentLength = Convert.ToInt32(request.Headers[HTTPHeader.ContentLength]);
                if (contentLength != 0)
                {
                    if (!request.Headers.ContainsKey(HTTPHeader.ContentType))
                    {
                        throw new HandlingException(HTTPStatusCode.BadRequest, "Bad Request");
                    }
                    if (request.Headers[HTTPHeader.ContentType].StartsWith(MIMETypes.Application.XWWWFormUrlEncoded))
                    {
                        // We might still be receiving client data at this point,
                        // which means that the reader.Read call will not always
                        // be able to read -all- of the data. So we loop until all
                        // the content is received!
                        char[] requestBody = new char[contentLength];
                        int charsRead = 0;
                        while (charsRead < contentLength)
                        {
                            int amountOfCharsRead = reader.Read(requestBody, charsRead, contentLength - charsRead);
                            charsRead += amountOfCharsRead;
                            if (amountOfCharsRead == 0 && charsRead != contentLength)
                                throw new HandlingException(HTTPStatusCode.BadRequest, "Wrong Content-Length");
                        }
                        string bodyAsString = new String(requestBody);
                        NameValueCollection body = HttpUtility.ParseQueryString(bodyAsString, clientEncoding);
                        request.Body = body;
                    }
                    else
                    {
                        throw new HandlingException(HTTPStatusCode.NotImplemented, "Not Supported");
                    }
                }
            }
        }

        void HTTPResponse_Error(object sender, HTTPResponseErrorEventArgs e)
        {
            OnError(e);
        }

        protected virtual void OnLog(HTTPRequest request, HTTPResponse response)
        {
            Log?.Invoke(this, new LogEventArgs(request, response));
        }

        protected virtual void OnError(EventArgs args)
        {
            Error?.Invoke(this, args);
        }

        private void _routeSolver_Error(object sender, RoutingErrorEventArgs e)
        {
            OnError(e);
        }

        [Serializable]
        private class HandlingException : Exception
        {
            public HTTPStatusCode Status { get; private set; }
            public string ErrorMessage { get; private set; }
            public HandlingException(HTTPStatusCode status, string errorMessage)
            {
                Status = status;
                ErrorMessage = errorMessage;
            }
        }
        #endregion
    }

    public class WebServerEventArgs : EventArgs
    {
        public WebServerEventArgs(HTTPRequest request, HTTPResponse response)
        {
            Request = request;
            Response = response;
        }
        public HTTPRequest Request { get; private set; }
        public HTTPResponse Response { get; private set; }
    }

    public class WebServerStreamEventArgs : WebServerEventArgs
    {
        public WebServerStreamEventArgs(HTTPRequest request, HTTPResponse response, Stream stream) :
            base(request, response)
        {
            Stream = stream;
        }

        public Stream Stream { get; private set; }
    }

    public class LogEventArgs : WebServerEventArgs
    {
        public LogEventArgs(HTTPRequest request, HTTPResponse response) : base(request, response)
        {
        }
    }

    public class CorsRequestArgs : WebServerStreamEventArgs
    {
        public CorsRequestArgs(HTTPRequest request, HTTPResponse response, Stream stream) :
            base(request, response, stream)
        {
        }

        /// <value>
        /// null value means do not include the field in the response's header
        /// </value>
        public string AccessControlAllowHeadersValue { get; set; }
        /// <value>
        /// null value means do not include the field in the response's header
        /// </value>
        public TimeSpan? AccessControlMaxAgeValue { get; set; }
        /// <value>
        /// null value means do not include the field in the response's header
        /// </value>
        public string AccessControlAllowOriginValue { get; set; }
        /// <value>
        /// null value means do not include the field in the response's header
        /// </value>
        public string AccessControlAllowMethodsValue { get; set; }

        /// <summary>
        /// Set to true to execute the request normally, 
        /// Set to false to ignore the request (typically for preflight CORS request)
        /// </summary>
        public bool ExecuteRequest { get; set; }

        public bool IsPreflightCorsRequest { get; internal set; }
    }
}
