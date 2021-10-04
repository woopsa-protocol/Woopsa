using System;

namespace Woopsa
{
    [Serializable]
    public class WoopsaNotificationsLostException : WoopsaException
    {
        #region Constructors

        public WoopsaNotificationsLostException()
        {
        }
        public WoopsaNotificationsLostException(string message)
            : base(message)
        {
        }
        public WoopsaNotificationsLostException(string message, Exception innerException) : base(message, innerException)
        {
        }

        #endregion
    }
}
