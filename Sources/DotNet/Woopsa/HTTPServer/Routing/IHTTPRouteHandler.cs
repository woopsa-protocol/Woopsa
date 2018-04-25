using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            _delegate = requestDelegate;
            _acceptSubroutes = acceptSubroutes;
        }

        private HandleRequestDelegate _delegate;
        public HandleRequestDelegate Delegate
        {
            set { _delegate = value; }
            get { return _delegate; }
        }

        public void HandleRequest(HTTPRequest request, HTTPResponse response)
        {
            _delegate(request, response);
        }


        public bool AcceptSubroutes
        {
            get { return _acceptSubroutes; }
        }
        private bool _acceptSubroutes = false;
    }
}
