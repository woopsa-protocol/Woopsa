using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.System.Threading;

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
    public class WebServer : IDisposable
    {
        public const int DefaultPortHttp = 80;
        public const int DefaultPortHttps = 443;        

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
        public WebServer(int port = DefaultPortHttp)
        {
            Port = port;
            PreRouteProcessors = new List<PreRouteProcessor>();
            Routes = new RouteSolver();
            Routes.Error += _routeSolver_Error;
            _listeners = new List<StreamSocketListener>();

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

        /// <summary>
        /// Which TCP port the WebServer is currently listening on
        /// </summary>
        public int Port { get; private set; }

        public List<PreRouteProcessor> PreRouteProcessors { get; private set; }
        #endregion

        #region Private Members
        
        private StreamSocketListener _listener;
        private List<StreamSocketListener> _listeners;

        private Dictionary<string, HTTPMethod> _supportedMethods = new Dictionary<string, HTTPMethod>()
        {
            {"GET",     HTTPMethod.GET},
            {"POST",    HTTPMethod.POST},
            {"PUT",     HTTPMethod.PUT},
            {"DELETE",  HTTPMethod.DELETE},
            {"OPTIONS", HTTPMethod.OPTIONS}
        };

        private bool _abort = false;
        private bool _started = false;
        #endregion

        #region Public Methods

        /// <summary>
        /// Starts listening on the TCP socket and initiates the routing mechanism. At this point, the server become reachable by any client.
        /// </summary>
        public async Task StartAsync()
        {
            _started = true;
            _listener = new StreamSocketListener();
            _listener.ConnectionReceived += async (sender, args) => HandleRequest(sender, args);

            await _listener.BindServiceNameAsync(Port.ToString());
        }

        /// <summary>
        /// Shutdowns the server and stops listening for TCP connexions. At this point, the server becomes completely unreachable.
        /// It cannot be restarted.
        /// </summary>
        public void Shutdown()
        {
            if (!_abort)
            {
                lock (_listeners)
                    foreach (var listener in _listeners)
                        listener.Dispose();
                _abort = true;
            }
        }

        #endregion

        #region Private Methods
        
        private async void HandleRequest(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            HTTPRequest request = null;
            HTTPResponse response = null;

            using (Stream inputStream = args.Socket.InputStream.AsStreamForRead())
            {
                var stream = inputStream;
                foreach (PreRouteProcessor processor in PreRouteProcessors)
                    stream = processor.ProcessStream(stream);

                // Do not dispose the reader so that the inner stream stays open (no using)
                StreamReader reader = new StreamReader(stream, Encoding.UTF8, false, 4096, true);

                try
                {
                    response = new HTTPResponse();
                    string requestString = await reader.ReadLineAsync();

                    string[] parts = requestString.Split(' ');
                    if (parts.Length != 3)
                        throw new HandlingException(HTTPStatusCode.BadRequest, "Bad Request");

                    string method = parts[0].ToUpper();
                    string url = parts[1];
                    string version = parts[2].ToUpper();

                    //Check if the version is what we expect
                    if (version != "HTTP/1.1" && version != "HTTP/1.0")
                        throw new HandlingException(HTTPStatusCode.HttpVersionNotSupported, "HTTP Version Not Supported");

                    //Check if the method is supported
                    if (!_supportedMethods.ContainsKey(method))
                        throw new HandlingException(HTTPStatusCode.NotImplemented, method + " Method Not Implemented");

                    //Build the request object
                    request = new HTTPRequest(_supportedMethods[method], WebUtility.UrlDecode(url));

                    //Add all headers to the request object
                    await FillHeadersAsync(request, reader);

                    //Handle encoding for this request
                    Encoding clientEncoding = InferEncoding(request);

                    //Extract all the data from the URL (base and query)
                    ExtractQuery(request, clientEncoding);

                    //Extract and decode all the POST data
                    await ExtractPostAsync(request, reader, clientEncoding);

                    if (request.Headers.ContainsKey(HTTPHeader.Connection))
                        if (request.Headers[HTTPHeader.Connection].ToLower().Equals("close"))
                            response.SetHeader(HTTPHeader.Connection, "close");

                    // Send a response back
                    using (IOutputStream output = args.Socket.OutputStream)
                    using (Stream outstream = output.AsStreamForWrite())
                    {
                        //Pass this on to the route solver
                        Routes.HandleRequest(request, response, outstream);
                        OnLog(request, response);
                    }
                }
                catch (HandlingException e)
                {
                    if (response != null)
                    {
                        response.WriteError(e.Status, e.ErrorMessage);
                        OnLog(request, response);
                    }
                }
                catch (Exception e)
                {
                    if (response != null)
                    {
                        response.WriteError(HTTPStatusCode.InternalServerError, "Internal Server Error. " + e.Message);
                        OnLog(request, response);
                    }
                }
                finally
                {
                    reader.Dispose();
                }
            }
        }

        private async Task FillHeadersAsync(HTTPRequest request, StreamReader reader)
        {
            string headerLine;
            while ((headerLine = await reader.ReadLineAsync()) != null && !headerLine.Equals(""))
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
            string baseUrl;
            if (url.IndexOf('?') != -1)
            {
                string[] queryParts = url.Split('?');
                baseUrl = queryParts[0];
                request.Query = HTTPServerUtils.ParseQueryString(queryParts[1]);
            }
            else
            {
                baseUrl = WebUtility.UrlDecode(url);
            }
            request.BaseURL = baseUrl;
        }

        private async Task ExtractPostAsync(HTTPRequest request, StreamReader reader, Encoding clientEncoding)
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
                            int amountOfCharsRead = await reader.ReadAsync(requestBody, charsRead, contentLength - charsRead);
                            charsRead += amountOfCharsRead;
                            if (amountOfCharsRead == 0 && charsRead != contentLength)
                                throw new HandlingException(HTTPStatusCode.BadRequest, "Wrong Content-Length");
                        }
                        string bodyAsString = new string(requestBody);
                        request.Body = HTTPServerUtils.ParseQueryString(bodyAsString);
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
            if (Log != null)
            {
                Log(this, new LogEventArgs(request, response));
            }
        }

        protected virtual void OnError(EventArgs args)
        {
            if (Error != null)
            {
                Error(this, args);
            }
        }

        private void _routeSolver_Error(object sender, RoutingErrorEventArgs e)
        {
            OnError(e);
        }
        
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

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Shutdown();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    public class LogEventArgs : EventArgs
    {
        public LogEventArgs(HTTPRequest request, HTTPResponse response)
        {
            Request = request;
            Response = response;
        }
        public HTTPRequest Request { get; private set; }
        public HTTPResponse Response { get; private set; }
    }
}
