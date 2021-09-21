using System.Collections.Generic;

namespace Woopsa
{
    public interface IWoopsaContainer : IWoopsaElement
	{
		IEnumerable<IWoopsaContainer> Items { get; }
	}

}
