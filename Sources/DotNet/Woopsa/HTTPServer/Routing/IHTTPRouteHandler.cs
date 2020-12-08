namespace Woopsa
{
    public interface IHTTPRouteHandler
    {
        void HandleRequest(HTTPRequest request, HTTPResponse response);
        bool AcceptSubroutes { get; }
    }

    public delegate void HandleRequestDelegate(HTTPRequest request, HTTPResponse response);

    public class RouteHandlerDelegate : IHTTPRouteHandler
    {
        public RouteHandlerDelegate(HandleRequestDelegate requestDelegate, bool acceptSubroutes = false, bool acceptWebsockets = false)
        {
            Delegate = requestDelegate;
            AcceptSubroutes = acceptSubroutes;
        }

        public HandleRequestDelegate Delegate { get; set; }

        public void HandleRequest(HTTPRequest request, HTTPResponse response) => Delegate(request, response);

        public bool AcceptSubroutes { get; } = false;
    }
}
