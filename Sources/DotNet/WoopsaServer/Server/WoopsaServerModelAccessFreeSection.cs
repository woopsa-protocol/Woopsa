using System;

namespace Woopsa
{
    public sealed class WoopsaServerModelAccessFreeSection : IDisposable
    {
        #region Constructor

        internal WoopsaServerModelAccessFreeSection(EndpointWoopsa server)
        {
            _endPoint = server;
            server.ExecuteAfterWoopsaModelAccess();
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _endPoint.ExecuteBeforeWoopsaModelAccess();
        }

        #endregion

        #region Fields / Attributes

        private EndpointWoopsa _endPoint;

        #endregion
    }
}
