using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
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

    // TODO: This used to inherit from WoopsaException
    // Inheriting from InvalidOperationException allows this exception
    // to be raised in Getters/Setters, such as WoopsaJsonData indexers
	[Serializable]
	public class WoopsaInvalidOperationException : InvalidOperationException
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
}
