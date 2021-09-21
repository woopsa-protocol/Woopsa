using Microsoft.AspNetCore.Http;
using System;

namespace Woopsa
{
    public class RoutingErrorEventArgs : EventArgs
    {
        #region Constructor

        public RoutingErrorEventArgs(RoutingErrorType type, string error, HttpRequest request)
        {
            Type = type;
            Error = error;
            Request = request;
        }

        #endregion

        #region Properties

        public string Error { get; }
        public HttpRequest Request { get; }
        public RoutingErrorType Type { get; }

        #endregion
    }
}
