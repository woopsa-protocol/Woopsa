using System.Collections.Generic;
using System.Threading.Tasks;

namespace Woopsa
{
    public interface IWoopsaMethod : IWoopsaElement
	{
		IWoopsaValue Invoke(IWoopsaValue[] arguments);
		Task<IWoopsaValue> InvokeAsync(IWoopsaValue[] arguments);
		WoopsaValueType ReturnType { get; }
		IEnumerable<IWoopsaMethodArgumentInfo> ArgumentInfos { get; }
	}

}
