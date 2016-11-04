using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            _fullURL = fullURL;
            _baseURL = _fullURL;
            _method = method;
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
        public string FullURL { get { return _fullURL; } internal set { _fullURL = value; } }

        /// <summary>
        /// Provides the Base URL that was sent by the client. This URL is thus stripped
        /// of the query parameters (values preceded by the ? sign in a URL) but still contains
        /// the entire Subroute when Subrouting is allowed by a <see cref="WebServer.IHTTPRouteHandler"/>
        /// </summary>
        public string BaseURL { get { return _baseURL; } internal set { _baseURL = value; } }

        /// <summary>
        /// When Subrouting is allowed by a <see cref="WebServer.IHTTPRouteHandler"/>, this
        /// property returns the Subroute. For example, if the RouteSolver is configured to accept
        /// Subroutes on the <c>/public</c> url, and a request is made for <c>/public/files/hello_world.html</c>, then 
        /// this property will thus be <c>/files/hello_world.html</c>
        /// </summary>
        public string Subroute { get { return _subroute; } internal set { _subroute = value; } }

        /// <summary>
        /// Provides the HTTP Method that was requested by the client (GET, POST, PUT, etc.)
        /// </summary>
        public HTTPMethod Method { get { return _method; } internal set { _method = value; } }

        /// <summary>
        /// Provides the HTTP Method that was requested by the client, as a string (GET, POST, PUT, etc.)
        /// </summary>
        public string MethodAsString
        {
            get
            {
                switch (_method)
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
        public NameValueCollection Query { get { return _query; } internal set { _query = value; } }

        /// <summary>
        /// Provides all the variables that were sent by the client in the POST request. Note:
        /// only POST requests encoded with <c>xwwww-form-urlencoded</c> method are supported. File uploads,
        /// which are generally encoded with <c>multipart/form-data</c>, are not supported.
        /// </summary>
        public NameValueCollection Body { get { return _body; } internal set { _body = value; } }
        #endregion

        #region Private/Protected/Internal Members
        private ReadOnlyHeaderDictionary _readOnlyHeaders = null;

        private string _fullURL;
        private string _baseURL;
        private string _subroute;
        private HTTPMethod _method;
        internal Dictionary<string, string> _headers;

        private NameValueCollection _query;
        private NameValueCollection _body;
        #endregion
    }

    public class ReadOnlyHeaderDictionary
    {
        public ReadOnlyHeaderDictionary(Dictionary<string, string> headers)
        {
            _headers = headers;
        }

        public string this[string key]
        {
            get { return _headers[key.ToLower()]; }
        }
        
        public bool ContainsKey(string key)
        {
            return _headers.ContainsKey(key.ToLower());
        }

        private Dictionary<string, string> _headers;
    }
}