using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Woopsa;

namespace WoopsaTest
{
	[TestClass]
	public class UnitTestWoopsaConverters
	{
		[TestMethod]
		public void TestWoopsaConverters()
		{
			var converters = new WoopsaConverters();
			converters.RegisterConverter(typeof(decimal), WoopsaConverterDecimal.Converter, WoopsaValueType.Real);
			var resut = converters.InferWoopsaType(typeof(decimal), out var valueType, out var converter);
			Assert.IsTrue(resut);
			Assert.IsNotNull(converter);
			Assert.AreEqual(WoopsaValueType.Real, valueType);
			Assert.AreEqual(typeof(WoopsaConverterDecimal), converter.GetType());
		}
	}

	class WoopsaConverterDecimal : WoopsaConverterDefault
	{
		static WoopsaConverterDecimal()
		{
			Converter = new WoopsaConverterDecimal();
		}

		public static readonly WoopsaConverterDecimal Converter;

		public override object FromWoopsaValue(IWoopsaValue value, Type targetType)
		{
			if (targetType == typeof(decimal))
				return decimal.Parse(value.AsText);
			else
				return base.FromWoopsaValue(value, targetType);
		}

		public override object GetDefaultValue(Type type)
		{
			return 0M;
		}
	}
}
