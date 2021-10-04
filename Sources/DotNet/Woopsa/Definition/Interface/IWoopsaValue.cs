using System;

namespace Woopsa
{
    public interface IWoopsaValue
	{
		string AsText { get; }
		WoopsaValueType Type { get; }
		DateTime? TimeStamp { get; }
	}

}
