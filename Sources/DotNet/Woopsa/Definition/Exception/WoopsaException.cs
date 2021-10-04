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
		#region Constructor

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

		#endregion
	}
}
