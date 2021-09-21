using Microsoft.AspNetCore.Http;
using System;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace Woopsa
{
    public abstract class BaseAuthenticator
    {
        #region Constructor

        public BaseAuthenticator(string realm)
        {
            Realm = realm;
        }

        #endregion

        #region Properties

        public string Realm { get; }

        #endregion

        #region Public Methods

        public virtual bool IsAuthenticated(HttpRequest request, HttpResponse response)
        {
            string username = null;
            string password = null;
            bool authenticated;

            if (request.Headers.ContainsKey(HTTPHeader.Authorization))
            {
                string authHeader = request.Headers[HTTPHeader.Authorization];
                if (authHeader != null)
                {
                    var authHeaderVal = AuthenticationHeaderValue.Parse(authHeader);

                    // RFC 2617 sec 1.2, "scheme" name is case-insensitive
                    if (authHeaderVal.Scheme.Equals("basic", StringComparison.OrdinalIgnoreCase) && authHeaderVal.Parameter != null)
                    {
                        var encoding = Encoding.GetEncoding("iso-8859-1");
                        string credentials = encoding.GetString(Convert.FromBase64String(authHeaderVal.Parameter));
                        int separator = credentials.IndexOf(':');
                        username = credentials.Substring(0, separator);
                        password = credentials[(separator + 1)..];
                    }
                }
            }
            authenticated = AuthenticateUser(request, username, password);
            if (authenticated)
            {
                var identity = new GenericIdentity(username);
                Thread.CurrentPrincipal = new GenericPrincipal(identity, null);
            }
            return authenticated;
        }


        #endregion

        #region Protected Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="username">null if no Authorization received in HTTP headers</param>
        /// <param name="password">null if no Authorization received in HTTP headers</param>
        /// <returns></returns>
        protected abstract bool AuthenticateUser(HttpRequest request, string username, string password);

        #endregion

        #region Fields / Attributes

        protected static AsyncLocal<string> _currentUserName = new AsyncLocal<string>();

        /// <summary>
        /// This property returns the username provided to authenticate a request within the current 
        /// webserver thread, or null if none. The value is specific to the calling thread, the property
        /// returns a different value in each thread.
        /// </summary>
        public static string CurrentUserName => _currentUserName.Value;

        #endregion
    }
}
