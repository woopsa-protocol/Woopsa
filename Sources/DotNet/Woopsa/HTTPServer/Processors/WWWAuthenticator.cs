using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public class WWWAuthenticator:PostRouteProcessor, IRequestProcessor
    {
        public WWWAuthenticator(string realm)
        {
            Realm = realm;
        }

        public delegate bool Check(string username, string password);

        public Check DoCheck { get; set; }
        public string Realm { get; set; }

        public bool Process(HTTPRequest request, HTTPResponse response)
        {
            if ( DoCheck == null )
            {
                throw new NotImplementedException("No DoCheck delegate was specified. What am I supposed to do?");
            }

            if ( !request.Headers.ContainsKey(HTTPHeader.Authorization) )
            {
                response.SetHeader(HTTPHeader.WWWAuthenticate, "Basic Realm=\"" + Realm + "\"");
                response.WriteError(HTTPStatusCode.Unauthorized, "Unauthorized");
                return false;
            }
            else
            {
                string authString = request.Headers[HTTPHeader.Authorization].Split(' ')[1];
                authString = Encoding.GetEncoding("ISO-8859-1").GetString(Convert.FromBase64String(authString));
                string[] parts = authString.Split(':');
                string username = parts[0];
                string password = parts[1];

                if (DoCheck(username, password))
                {
                    return true;
                }
                else
                {
                    response.SetHeader(HTTPHeader.WWWAuthenticate, "Basic Realm=\"" + Realm + "\"");
                    response.WriteError(HTTPStatusCode.Unauthorized, "Unauthorized");
                    return false;
                }
            }
        }
    }
}
