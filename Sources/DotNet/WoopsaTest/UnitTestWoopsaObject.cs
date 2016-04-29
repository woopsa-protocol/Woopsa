using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Woopsa;
using System.Linq;

namespace WoopsaTest
{
	[TestClass]
	public class UnitTestWoopsaObject
	{
		private double _minLevel, _maxLevel;

		[TestMethod]
		public void TestMethodWoopsaObjects()
		{
			WoopsaRoot root = new WoopsaRoot();
			WoopsaObject tunnel1 = new WoopsaObject(root, "Tunnel1");
            
			Assert.AreEqual(root.Items.Count(), 1);
			WoopsaObject tunnel2 = new WoopsaObject(root, "Tunnel2");
			Assert.AreEqual(root.Items.Count(), 2);
			WoopsaObject coMessung1 = new WoopsaObject(tunnel1, "CoMessung1");
			Assert.AreEqual(coMessung1.GetPath(), "/Tunnel1/CoMessung1");

            WoopsaProperty property1 = new WoopsaProperty(coMessung1, "Level", WoopsaValueType.Real,
                (sender) => 1040.0);
            int property2Value = 0;
            WoopsaProperty property2 = new WoopsaProperty(coMessung1, "Variation", WoopsaValueType.Real,
                (sender) => property2Value, (sender, value) => property2Value = value.ToInt32());

            Assert.AreEqual(coMessung1.Properties.Count(), 2);
			Assert.AreEqual(coMessung1.Properties.First().Value.ToDouble(), 1040.0);
            coMessung1.Properties.ByName("Variation").Value = 45;
            Assert.AreEqual(coMessung1.Properties.ByName("Variation").Value.ToInt32(), 45);
            Assert.AreEqual(coMessung1.Properties.ByName("Variation").Value.ToString(), "45");
            (coMessung1.ByName("Variation") as IWoopsaProperty).Value = (WoopsaValue)36;
            Assert.AreEqual(coMessung1.Properties.ByName("Variation").Value.ToInt32(), 36);
            coMessung1.Properties["Variation"].Value = 5;
            Assert.AreEqual(property2Value, 5);
            int variation = coMessung1.Properties["Variation"].Value;
            Assert.AreEqual(variation, 5);
            WoopsaMethod method1 = new WoopsaMethod(coMessung1, "Calibrate", WoopsaValueType.Null,
				new WoopsaMethodArgumentInfo[] { 
					new WoopsaMethodArgumentInfo("minLevel", WoopsaValueType.Real), 
					new WoopsaMethodArgumentInfo("maxLevel", WoopsaValueType.Real)
				},
				Calibrate);
			IWoopsaValue result = method1.Invoke(1.1, 5.5);
			Assert.AreEqual(result, WoopsaValue.Null);
			Assert.AreEqual(_minLevel, 1.1);
			Assert.AreEqual(_maxLevel, 5.5);
		}

		private WoopsaValue Calibrate(System.Collections.Generic.IEnumerable<IWoopsaValue> Arguments)
		{
			_minLevel = Arguments.ElementAt(0).ToDouble();
			_maxLevel = Arguments.ElementAt(1).ToDouble();
			return WoopsaValue.Null;
		}
	}
}
