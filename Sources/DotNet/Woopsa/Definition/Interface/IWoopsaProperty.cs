namespace Woopsa
{
    public interface IWoopsaProperty : IWoopsaElement
	{
		bool IsReadOnly { get; }
		IWoopsaValue Value { get; set; }
		WoopsaValueType Type { get; }
	}

}
