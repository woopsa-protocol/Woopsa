using System;

namespace Woopsa
{
    public sealed class ExceptionEventArgs : EventArgs
    {
        #region Constructor

        public ExceptionEventArgs(Exception exception)
        {
            Exception = exception;
        }

        #endregion

        #region Properties

        public Exception Exception { get; }

        #endregion
    }
}
