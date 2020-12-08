using System.Collections.Generic;
using System.Collections.Specialized;

namespace Woopsa
{
    /// <summary>
    /// Provides a mechanism to get information about an HTTP Request made by a client.
    /// <para>
    /// This class is created internally by the Web Server and thus cannot be created from
    /// outside code. To handle requests and response, the <see cref="WebServer.IHTTPRouteHandler"/> 
    /// interface or the delegate methods in <see cref="RouteSolver"/> which is available from the
    /// <see cref="WebServer.WebServer.Routes"/> property.
    /// </para>
    /// </summary>
    public class HTTPRequest
    {
        #region ctor
        internal HTTPRequest(HTTPMethod method, string fullURL)
        {
            FullURL = fullURL;
            BaseURL = FullURL;
            Method = method;
            _headers = new Dictionary<string, string>();
        }
        #endregion

        #region Public Members
        /// <summary>
        /// Provides all the HTTP Headers that were sent by the client in a key/value list.
        /// </summary>
        public ReadOnlyHeaderDictionary Headers
        {
            get
            {
                if (_readOnlyHeaders == null)
                {
                    _readOnlyHeaders = new ReadOnlyHeaderDictionary(_headers);
                }
                return _readOnlyHeaders;
            }
        }

        /// <summary>
        /// Provides the Full URL that was sent by the client. This URL thus contains
        /// the query parameters (values preceded by the ? sign in a URL) as well as the
        /// Subroute when Subrouting is allowed by a <see cref="WebServer.IHTTPRouteHandler"/>
        /// </summary>
        public string FullURL { get; internal set; }

        /// <summary>
        /// Provides the Base URL that was sent by the client. This URL is thus stripped
        /// of the query parameters (values preceded by the ? sign in a URL) but still contains
        /// the entire Subroute when Subrouting is allowed by a <see cref="WebServer.IHTTPRouteHandler"/>
        /// </summary>
        public string BaseURL { get; internal set; }

        /// <summary>
        /// When Subrouting is allowed by a <see cref="WebServer.IHTTPRouteHandler"/>, this
        /// property returns the Subroute. For example, if the RouteSolver is configured to accept
        /// Subroutes on the <c>/public</c> url, and a request is made for <c>/public/files/hello_world.html</c>, then 
        /// this property will thus be <c>/files/hello_world.html</c>
        /// </summary>
        public string Subroute { get; internal set; }

        /// <summary>
        /// Provides the HTTP Method that was requested by the client (GET, POST, PUT, etc.)
        /// </summary>
        public HTTPMethod Method { get; internal set; }

        /// <summary>
        /// Provides the HTTP Method that was requested by the client, as a string (GET, POST, PUT, etc.)
        /// </summary>
        public string MethodAsString
        {
            get
            {
                switch (Method)
                {
                    case HTTPMethod.GET:
                        return "GET";
                    case HTTPMethod.POST:
                        return "POST";
                    case HTTPMethod.HEAD:
                        return "HEAD";
                }
                return null;
            }
        }

        /// <summary>
        /// Provides all the variables that were sent by the client in the Query String, which is
        /// part of the URL (this is the part of the URL that follows the question mark (?) sign)
        /// </summary>
        public NameValueCollection Query { get; internal set; }

        /// <summary>
        /// Provides all the variables that were sent by the client in the POST request. Note:
        /// only POST requests encoded with <c>xwwww-form-urlencoded</c> method are supported. File uploads,
        /// which are generally encoded with <c>multipart/form-data</c>, are not supported.
        /// </summary>
        public NameValueCollection Body { get; internal set; }
        #endregion

        #region Private/Protected/Internal Members
        private ReadOnlyHeaderDictionary _readOnlyHeaders = null;
        internal Dictionary<string, string> _headers;
        #endregion
    }

    public class ReadOnlyHeaderDictionary
    {
        public ReadOnlyHeaderDictionary(Dictionary<string, string> headers)
        {
            _headers = headers;
        }

        public string this[string key] => _headers[key.ToLower()];

        public bool ContainsKey(string key) => _headers.ContainsKey(key.ToLower());

        private readonly Dictionary<string, string> _headers;
    }
}