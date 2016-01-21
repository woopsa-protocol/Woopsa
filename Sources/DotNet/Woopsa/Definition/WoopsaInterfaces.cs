using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{

	public static class WoopsaConst
	{
		public const char UrlSeparator = '/';
		public const char WoopsaPathSeparator = UrlSeparator;
		public const char WoopsaLinkSeparator = '#';
        public const string WoopsaRootPath = "/";

		public const string WoopsaFalse = "false";
		public const string WoopsaTrue = "true";
		public const string WoopsaNull = "null";
	}

	public enum WoopsaValueType 
	{ 
		Null, Logical, Integer, Real, DateTime, TimeSpan, Text, WoopsaLink, JsonData, ResourceUrl
	}

	public interface IWoopsaValue
	{
		string AsText { get; }
		WoopsaValueType Type { get; }
		DateTime? TimeStamp { get; }
	}
		
    public interface IWoopsaElement
    {
		/// <remarks>Is null for root nodes</remarks>
		IWoopsaContainer Owner { get; }

		string Name { get; }
    }

	public interface IWoopsaContainer : IWoopsaElement
	{
		IEnumerable<IWoopsaContainer> Items { get; }
	}

	public interface IWoopsaProperty : IWoopsaElement
	{
		bool IsReadOnly { get; }
		IWoopsaValue Value { get; set; }
		WoopsaValueType Type { get; }
	}

	public interface IWoopsaMethodArgumentInfo
	{
		string Name { get; }
		WoopsaValueType Type { get; }
	}

	public interface IWoopsaMethod : IWoopsaElement
	{
		IWoopsaValue Invoke(IEnumerable<IWoopsaValue> arguments);
		WoopsaValueType ReturnType { get; }
		IEnumerable<IWoopsaMethodArgumentInfo> ArgumentInfos { get; }
	}

	public interface IWoopsaObject : IWoopsaContainer
	{
		IEnumerable<IWoopsaProperty> Properties { get; }
		IEnumerable<IWoopsaMethod> Methods { get; }
	}

}
