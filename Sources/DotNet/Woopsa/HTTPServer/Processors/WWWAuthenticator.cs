using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public delegate bool AuthenticationCheck(string username, string password);

    public class WWWAuthenticator : PostRouteProcessor, IRequestProcessor
    {
        public WWWAuthenticator(string realm, AuthenticationCheck authenticationCheck)
        {
            Realm = realm;
            if (authenticationCheck != null)
                AuthenticationCheck = authenticationCheck;
            else
                throw new NotImplementedException("No DoCheck delegate was specified for WWWAuthenticator.");
        }

        public string Realm { get; private set; }

        public bool Process(HTTPRequest request, HTTPResponse response)
        {
            bool authenticated;
            if (request.Headers.ContainsKey(HTTPHeader.Authorization))
            {
                string authString = request.Headers[HTTPHeader.Authorization].Split(' ')[1];
                authString = Encoding.GetEncoding("ISO-8859-1").GetString(Convert.FromBase64String(authString));
                string[] parts = authString.Split(':');
                string username = parts[0];
                string password = parts[1];
                authenticated = AuthenticationCheck(username, password);
            }
            else
                authenticated = false;
            if (!authenticated)
            { 
                response.SetHeader(HTTPHeader.WWWAuthenticate, "Basic Realm=\"" + Realm + "\"");
                response.WriteError(HTTPStatusCode.Unauthorized, "Unauthorized");
            }
            return authenticated;
        }

        private AuthenticationCheck AuthenticationCheck;
    }
}
