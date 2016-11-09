
namespace Woopsa
{
    public enum WoopsaRedirectionType
    {
        Temporary = 1,
        Permanent = 2
    }
    public class RouteHandlerRedirect : IHTTPRouteHandler
    {
        public RouteHandlerRedirect(string targetLocation, WoopsaRedirectionType redirectionType)
        {
            _targetLocation = targetLocation;
            _redirectionType = redirectionType;
        }

        #region IHTTPRouteHandler
        public bool AcceptSubroutes
        {
            get { return true; }
        }

        public void HandleRequest(HTTPRequest request, HTTPResponse response)
        {
            if (_redirectionType == WoopsaRedirectionType.Temporary)
                response.SetStatusCode((int)HTTPStatusCode.TemporaryRedirect, "Temporary redirect");
            else if (_redirectionType == WoopsaRedirectionType.Permanent)
                response.SetStatusCode((int)HTTPStatusCode.Moved, "Permanent redirection");
            response.SetHeader("Location", _targetLocation);
        }
        #endregion IHTTPRouteHandler

        private string _targetLocation;
        private WoopsaRedirectionType _redirectionType;
    }
}
