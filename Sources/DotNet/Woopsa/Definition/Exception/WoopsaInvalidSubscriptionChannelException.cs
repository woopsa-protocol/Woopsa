using System;

namespace Woopsa
{
    [Serializable]
    public class WoopsaInvalidSubscriptionChannelException : WoopsaException
    {
        #region Constructors

        public WoopsaInvalidSubscriptionChannelException()
        {
        }
        public WoopsaInvalidSubscriptionChannelException(string message)
            : base(message)
        {
        }
        public WoopsaInvalidSubscriptionChannelException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        #endregion
    }
}
