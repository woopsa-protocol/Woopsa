using Microsoft.AspNetCore.Http;
using System;

namespace Woopsa
{
    public delegate void AuthenticationCheck(object sender, AuthenticationCheckEventArgs e);

    public class SimpleAuthenticator : BaseAuthenticator
    {
        #region Constructor

        public SimpleAuthenticator(string realm, AuthenticationCheck authenticationCheck) :
            base(realm)
        {
            if (authenticationCheck != null)
                _authenticationCheck = authenticationCheck;
            else
                throw new NotImplementedException("No DoCheck delegate was specified for WWWAuthenticator.");
        }

        #endregion

        #region Protected Methods

        protected override bool AuthenticateUser(HttpRequest request, string username, string password)
        {
            _currentUserName.Value = null;
            AuthenticationCheckEventArgs eventArgs = new(request, username, password)
            {
                IsAuthenticated = false
            };
            _authenticationCheck(this, eventArgs);
            if (eventArgs.IsAuthenticated)
            {
                _currentUserName.Value = username;
            }
            return eventArgs.IsAuthenticated;
        }

        #endregion

        #region Fields / Attributes

        private readonly AuthenticationCheck _authenticationCheck;

        #endregion
    }
}
