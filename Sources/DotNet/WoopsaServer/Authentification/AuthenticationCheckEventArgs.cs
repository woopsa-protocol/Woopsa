using Microsoft.AspNetCore.Http;
using System;

namespace Woopsa
{
    public class AuthenticationCheckEventArgs : EventArgs
    {
        #region Constructor

        public AuthenticationCheckEventArgs(HttpRequest request, string username, string password)
        {
            Request = request;
            Username = username;
            Password = password;
        }

        #endregion

        #region Properties

        public HttpRequest Request { get; }
        public string Username { get; }
        public string Password { get; }

        public bool IsAuthenticated { get; set; }

        #endregion
    }
}
