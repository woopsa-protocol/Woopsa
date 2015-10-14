using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Woopsa
{
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
    public class WebServer
    {
        public const int DEFAULT_PORT_HTTP = 80;
        public const int DEFAULT_PORT_HTTPS = 443;

        /// <summary>
        /// Creates a WebServer that runs on port 80, single-threaded
        /// </summary>
        /// <remarks>
        /// A server must be multithreaded in order to use the Keep-Alive HTTP mechanism.
        /// <seealso cref="WebServer.WebServer(int port, bool multiThreaded)"/>
        /// </remarks>
        public WebServer() : this(DEFAULT_PORT_HTTP, false) { }

        /// <summary>
        /// Creates a WebServer that runs on the specified port, single-threaded
        /// </summary>
        /// <param name="port">
        /// The port on which to run the server (default 80)
        /// </param>
        /// <remarks> 
        /// A server must be multithreaded in order to use the Keep-Alive HTTP mechanism.
        /// <seealso cref="WebServer.WebServer(int port, bool multiThreaded)"/>
        /// </remarks>
        public WebServer(int port) : this(port, false) { }

        /// <summary>
        /// Creates a WebServer that runs on the specified port and can be multithreaded
        /// </summary>
        /// <param name="port">
        /// The port on which to run the server (default 80)
        /// </param>
        /// <param name="multithreaded">
        /// Whether this server should use a ThreadPool or not.
        /// </param>
        /// <remarks>
        /// A server must be multithreaded in order to use the Keep-Alive HTTP mechanism.
        /// </remarks>
        public WebServer(int port, bool multithreaded)
        {
            Port = port;
            _routeSolver = new RouteSolver();
            _routeSolver.Error += _routeSolver_Error;
            _listener = new TcpListener(IPAddress.Any, port);
            MultiThreaded = multithreaded;
            _listenerThread = new Thread(Listen);
            _listenerThread.Name = "WebServer_Listener";
            HTTPResponse.Error += HTTPResponse_Error;
        }

        #region Public Members
        /// <summary>
        /// The RouteSolver allows a user to configure routes on the web server. This member is created internally and as such is read-only.
        /// </summary>
        public RouteSolver Routes { get { return _routeSolver; } }

        /// <summary>
        /// This event is raised whenever an orrur occurs inside the web server. In most cases, the error can be ignored, but odd behavior might occur when there are multiple matching routes, for example.
        /// </summary>
        public delegate void ErrorEventHandler(object sender, EventArgs e);
        public event ErrorEventHandler Error;

        public event EventHandler<LogEventArgs> Log;

        /// <summary>
        /// Whether this server is using a thread pool to handle requests
        /// </summary>
        public bool MultiThreaded { get; private set; }

        public int Port { get; private set; }
        #endregion

        #region Private Members
        private RouteSolver _routeSolver;
        private TcpListener _listener;
        private Thread _listenerThread;

        private Dictionary<string, HTTPMethod> _supportedMethods = new Dictionary<string, HTTPMethod>()
        {
            {"GET",     HTTPMethod.GET},
            {"POST",    HTTPMethod.POST},
            {"PUT",     HTTPMethod.PUT},
            {"DELETE",  HTTPMethod.DELETE}
        };

        private bool _abort = false;
        private bool _started = false;
        #endregion

        #region Public Methods
        /// <summary>
        /// Starts listening on the TCP socket and initiates the routing mechanism. At this point, the server become reachable by any client.
        /// </summary>
        public void Start()
        {
            _listenerThread.Start();
            _listener.Start();
            _started = true;
        }

        /// <summary>
        /// Stops the server and stops listening for TCP connexions. At this point, the server becomes completely unreachable.
        /// </summary>
        public void Stop()
        {
            _listener.Stop();
            _abort = true;
        }
        #endregion

        #region Private Methods
        private void Listen()
        {
            while(!_abort && _started)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    if (MultiThreaded)
                    {
                        ThreadPool.QueueUserWorkItem((o) => HandleClient(client.GetStream()));
                    }
                    else
                    {
                        HandleClient(client.GetStream());
                    }
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.Interrupted)
                    {
                        _abort = true;
                    }
                }
            }
            _listener.Stop();
        }

        private void HandleClient(Stream stream)
        {
            StreamReader reader = new StreamReader(stream, Encoding.UTF8, false, 4096, true);
            bool leaveOpen = true;
            stream.ReadTimeout = 2000;
            HTTPResponse response = null;
            HTTPRequest request = null;

            try
            {
                while (leaveOpen && !_abort)
                {
                    response = new HTTPResponse();
                    /*
                        * Parse the first line of the HTTP Request
                        * Examples:
                        *      GET / HTTP/1.1
                        *      POST /submit HTTP/1.1
                        */
                    string requestString = null;
                    try
                    {
                        requestString = reader.ReadLine();
                    }
                    catch (Exception)
                    {
                        leaveOpen = false;
                        break;
                    }
                    if (requestString == null)
                    {
                        //Why does this happen?
                        break;
                    }


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
                    if (request.Headers.ContainsKey(HTTPHeader.Connection))
                    {
                        if (request.Headers[HTTPHeader.Connection].ToLower().Equals("keep-alive"))
                        {
                            keepAlive = true;
                            response.SetHeader(HTTPHeader.Connection, "keep-alive");
                        }
                    }

                    //Keep-Alive can only work on a multithreaded server!
                    if (!keepAlive || !MultiThreaded)
                    {
                        leaveOpen = false;
                        response.SetHeader(HTTPHeader.Connection, "close");
                    }
                    //Pass this on to the route solver
                    _routeSolver.HandleRequest(request, response, stream);
                    OnLog(request, response);
                }
            }
            catch (HandlingException e)
            {
                if ( response != null )
                {
                    response.WriteError(e.Status, e.ErrorMessage);
                    response.Respond(stream);
                    OnLog(request, response);
                }
            }
            catch (Exception e)
            {
                if (response != null)
                {
                    response.WriteError(HTTPStatusCode.InternalServerError, "Internal Server Error. " + e.Message);
                    response.Respond(stream);
                    OnLog(request, response);
                }
            }
            finally
            {
                reader.Close();
                stream.Close();
            }
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
                request._headers.Add(newHeader[0].Replace(" ", ""), newHeader[1].Trim());
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
                baseUrl = WebUtility.UrlDecode(url);
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
                int contentLength =  Convert.ToInt32(request.Headers[HTTPHeader.ContentLength]);
                if (contentLength != 0 )
                {
                    if (!request.Headers.ContainsKey(HTTPHeader.ContentType))
                    {
                        throw new HandlingException(HTTPStatusCode.BadRequest, "Bad Request");
                    }
                    if (request.Headers[HTTPHeader.ContentType].StartsWith(MIMETypes.Application.XWWWFormUrlEncoded))
                    {
                        char[] requestBody = new char[contentLength];
                        reader.Read(requestBody, 0, contentLength);
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
            if ( Log != null )
            {
                Log(this, new LogEventArgs(request, response));
            }
        }

        protected virtual void OnError(EventArgs args)
        {
            if ( Error != null )
            {
                Error(this, args);
            }
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

    public class LogEventArgs : EventArgs
    {
        public HTTPRequest Request;
        public HTTPResponse Response;
        public LogEventArgs(HTTPRequest request, HTTPResponse response)
        {
            Request = request;
            Response = response;
        }
    }
}
