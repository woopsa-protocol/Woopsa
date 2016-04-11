namespace Woopsa
{
    public enum WoopsaRedirection
    {
        Temporary = 1,
        Premanent = 2
    }
    public class RouteHandelerRedirect : IHTTPRouteHandler
    {
        public RouteHandelerRedirect(string location, WoopsaRedirection redirection)
        {
            _location = location;
            _redirection = redirection;

        }
        public bool AcceptSubroutes
        {
            get
            {
                return true;
            }
        }

        public void HandleRequest(HTTPRequest request, HTTPResponse response)
        {
            if(_redirection == WoopsaRedirection.Temporary)
                response.SetStatusCode((int)HTTPStatusCode.TemporaryRedirect, "Temporary redirect");
            else if(_redirection == WoopsaRedirection.Temporary)
                response.SetStatusCode((int)HTTPStatusCode.Moved, "Permanent redirection");
            response.SetHeader("Location", _location);
        }

        private string _location;
        private WoopsaRedirection _redirection;
    }
}
