using System;
using System.Text;

namespace Woopsa
{
    public abstract class BaseAuthenticator : PostRouteProcessor, IRequestProcessor
    {
        public BaseAuthenticator(string realm)
        {
            Realm = realm;            
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
                authenticated = Authenticate(username, password);
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

        protected abstract bool Authenticate(string username, string password);
    }

    public class AuthenticationCheckEventArgs : EventArgs
    {
        public AuthenticationCheckEventArgs(string username, string password)
        {
            Username = username;
            Password = password;
        }
        public string Username { get; private set; }
        public string Password { get; private set; }

        public bool IsAuthenticated { get; set; }
    }

    public delegate void AuthenticationCheck(object sender, AuthenticationCheckEventArgs e);

    public class SimpleAuthenticator : BaseAuthenticator
    {
        public SimpleAuthenticator(string realm, AuthenticationCheck authenticationCheck): 
            base(realm)
        {
            if (authenticationCheck != null)
                AuthenticationCheck = authenticationCheck;
            else
                throw new NotImplementedException("No DoCheck delegate was specified for WWWAuthenticator.");
        }

        protected override bool Authenticate(string username, string password)
        {
            AuthenticationCheckEventArgs eventArgs = new AuthenticationCheckEventArgs(username, password);
            eventArgs.IsAuthenticated = false;
            AuthenticationCheck(this, eventArgs);
            return eventArgs.IsAuthenticated;
        }

        private AuthenticationCheck AuthenticationCheck;
    }

}
