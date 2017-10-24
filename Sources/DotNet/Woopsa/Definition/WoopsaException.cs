using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public static class WoopsaExceptionMessage
    {
        public static string WoopsaCastTypeMessage(string destinationType, string sourceType)
        {
            return string.Format("Cannot typecast woopsa value of type {0} to type {1}", sourceType, destinationType);
        }

        public static string WoopsaCastValueMessage(string destinationType, string sourceValue)
        {
            return string.Format("Cannot typecast woopsa value {0} to type {1}", sourceValue, destinationType);
        }

        public static string WoopsaElementNotFoundMessage(string path)
        {
            return string.Format("Cannot find WoopsaElement specified by path {0}", path);
        }
    }

	[Serializable]
	public class WoopsaException: Exception
	{
		public WoopsaException()
		{
		}
		public WoopsaException(string message)
			: base(message)
		{
		}
		public WoopsaException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}

    [Serializable]
    public class WoopsaNotificationsLostException : WoopsaException
    {
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
    }

    [Serializable]
    public class WoopsaInvalidSubscriptionChannelException : WoopsaException
    {
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
    }

	[Serializable]
	public class WoopsaNotFoundException : WoopsaException
    {
		public WoopsaNotFoundException()
		{
		}
		public WoopsaNotFoundException(string message)
			: base(message)
		{
		}
        public WoopsaNotFoundException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
    }

	[Serializable]
	public class WoopsaInvalidOperationException : WoopsaException
    {
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
    }

    public sealed class ExceptionEventArgs : EventArgs
    {
        public ExceptionEventArgs(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; private set; }
    }
}
