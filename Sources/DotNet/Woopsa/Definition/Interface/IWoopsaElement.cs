namespace Woopsa
{
    public interface IWoopsaElement
    {
		/// <remarks>Is null for root nodes</remarks>
		IWoopsaContainer Owner { get; }

		string Name { get; }
    }

}
