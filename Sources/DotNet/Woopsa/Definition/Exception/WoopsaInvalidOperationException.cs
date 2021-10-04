using System;

namespace Woopsa
{
    [Serializable]
	public class WoopsaInvalidOperationException : WoopsaException
    {
        #region Constructors

        public WoopsaInvalidOperationException()
        {
        }

        public WoopsaInvalidOperationException(string message)
            : base(message)
        {
        }

        public WoopsaInvalidOperationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        #endregion
    }
}
