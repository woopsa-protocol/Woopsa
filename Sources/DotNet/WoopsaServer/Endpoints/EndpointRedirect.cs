
using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Woopsa
{
    public enum WoopsaRedirectionType
    {
        Temporary = 1,
        Permanent = 2
    }

    public class EndpointRedirect : Endpoint
    {
        #region Constructor

        public EndpointRedirect(string routePrefix, HTTPMethod httpMethod, string targetLocation, WoopsaRedirectionType redirectionType)
            : base(routePrefix)
        {
            _targetLocation = targetLocation;
            _redirectionType = redirectionType;
            _httpMethod = httpMethod;
        }

        #endregion

        #region IHTTPRouteHandler

        public override async Task HandleRequestAsync(HttpContext context, string subRoute)
        {
            try
            {
                if ((_httpMethod & (HTTPMethod)Enum.Parse(typeof(HTTPMethod), context.Request.Method)) == 0)
                {
                    await context.Response.WriteErrorAsync(HttpStatusCode.NotFound, "Not Found");
                    return;
                }

                context.Response.SetHeader("Location", _targetLocation);

                if (_redirectionType == WoopsaRedirectionType.Temporary)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.TemporaryRedirect;
                    await context.Response.WriteAsync("Temporary redirect");
                }
                else if (_redirectionType == WoopsaRedirectionType.Permanent)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Moved;
                    await context.Response.WriteAsync("Permanent redirection");
                }
            }
            catch (System.Exception ex)
            {

                throw;
            }
        }

        #endregion IHTTPRouteHandler

        #region Fields / Attributes

        private string _targetLocation;
        private WoopsaRedirectionType _redirectionType;
        private HTTPMethod _httpMethod;

        #endregion
    }
}
