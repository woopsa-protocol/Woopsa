using System;

namespace Woopsa
{
    public sealed class WoopsaServerModelAccessLockedSection : IDisposable
    {
        #region Fields / Attributes

        private EndpointWoopsa _server;

        #endregion

        #region Internal Methods

        internal WoopsaServerModelAccessLockedSection(EndpointWoopsa server)
        {
            _server = server;
            server.ExecuteBeforeWoopsaModelAccess();
        }

        #endregion

        #region Public Methods

        public void Dispose()
        {
            _server.ExecuteAfterWoopsaModelAccess();
        }

        #endregion
    }
}
