using System;

namespace Woopsa
{
    [Serializable]
	public class WoopsaNotFoundException : WoopsaException
    {
		#region Constructors

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

		#endregion
	}
}
