using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Woopsa;
using System.Collections;

namespace WoopsaTest
{
    [WoopsaVisibility(WoopsaVisibility.DefaultIsVisible | WoopsaVisibility.Inherited | WoopsaVisibility.ObjectClassMembers)]
    public class ClassAInner1
    {
        public int APropertyInt { get; set; }

        [WoopsaVisible(false)]
        public int APropertyIntHidden { get; set; }

    }

    public class SubClassAInner1:ClassAInner1
    {
        public int ExtraProperty { get; set; }
    }

    public class ClassA
    {
        public ClassA()
        {
            Inner1 = new ClassAInner1();
        }
        public bool APropertyBool { get; set; }

        [WoopsaVisible(false)]
        public DateTime APropertyDateTime { get; set; }
        public DateTime APropertyDateTime2 { get; set; }

        public ClassAInner1 Inner1 { get; set; }
    }

    [WoopsaVisibility(WoopsaVisibility.DefaultIsVisible | WoopsaVisibility.Inherited | WoopsaVisibility.MethodSpecialName)]
    public class ClassB : ClassA
    {
        public int APropertyInt { get; set; }

        double APropertyDouble { get; set; }
    }

    [WoopsaVisibility(WoopsaVisibility.None)]
    public class ClassC : ClassB
    {
        [WoopsaVisible]
        [WoopsaValueType(WoopsaValueType.Integer)]
        public string APropertyText { get; set; }

        [WoopsaVisible]
        public TimeSpan APropertyTimeSpan { get; set; }

        // This one must not be published (private)
        [WoopsaVisible]
        private double APropertyDouble2 { get; set; }

        [WoopsaVisible]
        [WoopsaValueType(WoopsaValueType.JsonData)]
        public string APropertyJson { get; set; }


    }

    public class ClassD
    {
        public ClassD(int n) { APropertyInt = n; }
        public int APropertyInt { get; set; }
    }

    [TestClass]
    public class UnitTestWoopsaObjectAdapter
    {
        [TestMethod]
        public void TestWoopsaObjectAdapter()
        {
            
            ClassA a = new ClassA();
            WoopsaObjectAdapter adapterA1 = new WoopsaObjectAdapter(null, "a", a);
            Assert.IsNotNull(adapterA1.Properties.ByNameOrNull(nameof(a.APropertyBool)));
            Assert.AreEqual(adapterA1.Properties.ByNameOrNull(nameof(a.APropertyBool)).Value.Type, WoopsaValueType.Logical);
            Assert.IsFalse(adapterA1.Properties.ByNameOrNull(nameof(a.APropertyBool)).Value.ToBool());
            Assert.IsNull(adapterA1.Methods.ByNameOrNull(nameof(ClassA.ToString)));
            adapterA1.Properties.ByNameOrNull(nameof(a.APropertyBool)).Value = new WoopsaValue(true);
            Assert.IsTrue(a.APropertyBool);
            Assert.IsTrue(adapterA1.Properties.ByNameOrNull(nameof(a.APropertyBool)).Value.ToBool());
            Assert.IsNull(adapterA1.Properties.ByNameOrNull(nameof(a.APropertyBool)).Value.TimeStamp);
            Assert.IsNull(adapterA1.Properties.ByNameOrNull(nameof(a.APropertyDateTime)));
            Assert.IsNotNull(adapterA1.Properties.ByNameOrNull(nameof(a.APropertyDateTime2)));
            Assert.IsNotNull(adapterA1.Items.ByNameOrNull("Inner1"));
            IWoopsaObject inner1 = adapterA1.Items.ByName("Inner1") as IWoopsaObject;
            Assert.IsNotNull(inner1);
            Assert.IsNotNull(inner1.Properties.ByNameOrNull(nameof(ClassAInner1.APropertyInt)));
            Assert.IsNull(inner1.Properties.ByNameOrNull(nameof(ClassAInner1.APropertyIntHidden)));
            inner1.Properties.ByNameOrNull(nameof(ClassAInner1.APropertyInt)).Value = new WoopsaValue(5);
            Assert.AreEqual(a.Inner1.APropertyInt, 5);
            a.Inner1.APropertyInt = 12;
            Assert.AreEqual(inner1.Properties.ByNameOrNull(nameof(ClassAInner1.APropertyInt)).Value, 12);
            Assert.IsNull(inner1.Methods.ByNameOrNull(nameof(ClassAInner1.ToString)));

            // dynamic object change with polymorphism
            a.Inner1 = new ClassAInner1() { APropertyInt = 123, APropertyIntHidden = 0 };
            Assert.AreEqual(inner1.Properties.ByName(nameof(ClassAInner1.APropertyInt)).Value, 123);
            Assert.IsNull(inner1.Properties.ByNameOrNull(nameof(SubClassAInner1.ExtraProperty)));
            a.Inner1 = new SubClassAInner1() { APropertyInt = 123, APropertyIntHidden = 0,
                ExtraProperty=555 };
            Assert.AreEqual(inner1.Properties.ByName(nameof(SubClassAInner1.ExtraProperty)).Value, 555);

            WoopsaObjectAdapter adapterA1All = new WoopsaObjectAdapter(null, "a", a,
                WoopsaObjectAdapterOptions.None, WoopsaVisibility.All);
            Assert.IsNotNull(adapterA1All.Methods.ByNameOrNull(nameof(ClassA.ToString)));
            IWoopsaObject inner1All = adapterA1All.Items.ByName("Inner1") as IWoopsaObject;
            Assert.IsNotNull(inner1All.Methods.ByNameOrNull(nameof(ClassAInner1.ToString)));



            WoopsaObjectAdapter adapterA2 = new WoopsaObjectAdapter(null, "a", a, WoopsaObjectAdapterOptions.SendTimestamps);
            Assert.IsNotNull(adapterA2.Properties.ByNameOrNull(nameof(a.APropertyBool)).Value.TimeStamp);

            WoopsaObjectAdapter adapterA3 = new WoopsaObjectAdapter(null, "a", a, WoopsaObjectAdapterOptions.DisableClassesCaching);
            Assert.IsNotNull(adapterA1.Properties.ByNameOrNull(nameof(a.APropertyBool)));
            
            ClassB b = new ClassB();
            WoopsaObjectAdapter adapterB = new WoopsaObjectAdapter(null, "b", b,
                WoopsaObjectAdapterOptions.None, WoopsaVisibility.DefaultIsVisible | WoopsaVisibility.MethodSpecialName |
                WoopsaVisibility.Inherited);
            Assert.IsNotNull(adapterB.Methods.ByNameOrNull("get_" + nameof(ClassB.APropertyBool)));
            Assert.IsNotNull(adapterB.Properties.ByNameOrNull(nameof(b.APropertyBool)));

            ClassC c = new ClassC();
            WoopsaObjectAdapter adapterC = new WoopsaObjectAdapter(null, "c", c);
            Assert.IsNotNull(adapterC.Properties.ByNameOrNull(nameof(c.APropertyBool)));
            Assert.IsNotNull(adapterC.Properties.ByNameOrNull(nameof(c.APropertyTimeSpan)));
            Assert.IsNull(adapterC.Properties.ByNameOrNull("APropertyDouble2"));
            Assert.IsNotNull(adapterC.Properties.ByNameOrNull(nameof(c.APropertyText)));
            IWoopsaProperty propertyText = adapterC.Properties.ByNameOrNull(nameof(c.APropertyText));
            Assert.AreEqual(propertyText.Type, WoopsaValueType.Integer);
            c.APropertyText = "123";
            Assert.AreEqual(propertyText.Value.ToInt64(), 123);
            propertyText.Value = new WoopsaValue(456);
            Assert.AreEqual(c.APropertyText, "456");
            // Json data
            Assert.IsNotNull(adapterC.Properties.ByNameOrNull(nameof(c.APropertyJson)));
            IWoopsaProperty propertyJson = adapterC.Properties.ByNameOrNull(nameof(c.APropertyJson));
            Assert.AreEqual(propertyJson.Type, WoopsaValueType.JsonData);
            // JSon structure
            c.APropertyJson = "{ \"x\" : 8, \"y\": 9 }";
            Assert.IsTrue(propertyJson.Value is WoopsaValue);
            WoopsaValue jsonValue = (WoopsaValue)propertyJson.Value;
            Assert.IsNotNull(jsonValue.JsonData);
            Assert.AreEqual(jsonValue.JsonData["x"].ToInt64(), 8);
            Assert.AreEqual(jsonValue.JsonData["y"].ToInt64(), 9);
            // JSon array
            c.APropertyJson = "{ \"a\" : [11, 12, 13] }";
            Assert.IsTrue(propertyJson.Value is WoopsaValue);
            jsonValue = (WoopsaValue)propertyJson.Value;
            Assert.IsNotNull(jsonValue.JsonData);
            Assert.AreEqual(jsonValue.JsonData["a"][0].ToInt64(), 11);
            Assert.AreEqual(jsonValue.JsonData["a"][1].ToInt64(), 12);
            Assert.AreEqual(jsonValue.JsonData["a"][2].ToInt64(), 13);

            ClassD[] array = new ClassD[] { new ClassD(4), new ClassD(3), new ClassD(2) };
            WoopsaObjectAdapter adapterArrayObject = new WoopsaObjectAdapter(null, "array", array, WoopsaObjectAdapterOptions.None,
                WoopsaVisibility.IEnumerableObject | WoopsaVisibility.DefaultIsVisible);
            Assert.IsNotNull(adapterArrayObject.Items.ByNameOrNull("Item[1]"));
            Assert.IsNotNull(adapterArrayObject.Items.ByNameOrNull("Item[1]") as IWoopsaObject);
            IWoopsaObject item1 = (IWoopsaObject)adapterArrayObject.Items.ByNameOrNull("Item[1]");
            Assert.IsNotNull(item1.Properties.ByNameOrNull(nameof(ClassD.APropertyInt)));
            Assert.AreEqual(item1.Properties.ByNameOrNull(nameof(ClassD.APropertyInt)).Value.ToInt64(), 3);
            item1.Properties.ByNameOrNull(nameof(ClassD.APropertyInt)).Value = new WoopsaValue(5, DateTime.Now);
            Assert.AreEqual(array[1].APropertyInt, 5);
            Assert.AreEqual(item1.Properties.ByNameOrNull(nameof(ClassD.APropertyInt)).Value.ToInt64(), 5);

            int[] dataArray = new int[] { 3, 4, 5 };
            WoopsaObjectAdapter adapterArrayValue = new WoopsaObjectAdapter(null, "array", dataArray, WoopsaObjectAdapterOptions.None,
                WoopsaVisibility.IEnumerableObject | WoopsaVisibility.DefaultIsVisible);
            WoopsaMethod methodGet = adapterArrayValue.Methods.ByNameOrNull("Get");
            Assert.IsNotNull(methodGet);
            int dataItem1 = methodGet.Invoke(1);
            Assert.AreEqual(dataItem1, dataArray[1]);
            WoopsaMethod methodSet = adapterArrayValue.Methods.ByNameOrNull("Set");
            Assert.IsNotNull(methodSet);
            methodSet.Invoke(1, 7);
            dataItem1 = methodGet.Invoke(1);
            Assert.AreEqual(dataArray[1], 7);
            Assert.AreEqual(dataItem1, 7);
        }

        [TestMethod]
        public void TestWoopsaObjectAdapterExposedType()
        {
            // TODO : Cleanup what is redundant with TestWoopsaObjectAdapter
            ClassA a = new ClassA();
            WoopsaObjectAdapter adapterA1 = new WoopsaObjectAdapter(null, "a", a, typeof(ClassA));
            Assert.IsNotNull(adapterA1.Properties.ByNameOrNull(nameof(a.APropertyBool)));
            Assert.AreEqual(adapterA1.Properties.ByNameOrNull(nameof(a.APropertyBool)).Value.Type, WoopsaValueType.Logical);
            Assert.IsFalse(adapterA1.Properties.ByNameOrNull(nameof(a.APropertyBool)).Value.ToBool());
            Assert.IsNull(adapterA1.Methods.ByNameOrNull(nameof(ClassA.ToString)));
            adapterA1.Properties.ByNameOrNull(nameof(a.APropertyBool)).Value = new WoopsaValue(true);
            Assert.IsTrue(a.APropertyBool);
            Assert.IsTrue(adapterA1.Properties.ByNameOrNull(nameof(a.APropertyBool)).Value.ToBool());
            Assert.IsNull(adapterA1.Properties.ByNameOrNull(nameof(a.APropertyBool)).Value.TimeStamp);
            Assert.IsNull(adapterA1.Properties.ByNameOrNull(nameof(a.APropertyDateTime)));
            Assert.IsNotNull(adapterA1.Properties.ByNameOrNull(nameof(a.APropertyDateTime2)));
            Assert.IsNotNull(adapterA1.Items.ByNameOrNull("Inner1"));
            IWoopsaObject inner1 = adapterA1.Items.ByName("Inner1") as IWoopsaObject;
            Assert.IsNotNull(inner1);
            Assert.IsNotNull(inner1.Properties.ByNameOrNull(nameof(ClassAInner1.APropertyInt)));
            Assert.IsNull(inner1.Properties.ByNameOrNull(nameof(ClassAInner1.APropertyIntHidden)));
            inner1.Properties.ByNameOrNull(nameof(ClassAInner1.APropertyInt)).Value = new WoopsaValue(5);
            Assert.AreEqual(a.Inner1.APropertyInt, 5);
            a.Inner1.APropertyInt = 12;
            Assert.AreEqual(inner1.Properties.ByNameOrNull(nameof(ClassAInner1.APropertyInt)).Value, 12);
            Assert.IsNull(inner1.Methods.ByNameOrNull(nameof(ClassAInner1.ToString)));

            // dynamic object change with polymorphism
            a.Inner1 = new ClassAInner1() { APropertyInt = 123, APropertyIntHidden = 0 };
            Assert.AreEqual(inner1.Properties.ByName(nameof(ClassAInner1.APropertyInt)).Value, 123);
            Assert.IsNull(inner1.Properties.ByNameOrNull(nameof(SubClassAInner1.ExtraProperty)));
            a.Inner1 = new SubClassAInner1()
            {
                APropertyInt = 123,
                APropertyIntHidden = 0,
                ExtraProperty = 555
            };
            // Should not find this property, as we are using declared type instaed of actual type
            Assert.IsNull(inner1.Properties.ByNameOrNull(nameof(SubClassAInner1.ExtraProperty)));

            WoopsaObjectAdapter adapterA1All = new WoopsaObjectAdapter(null, "a", a,
                WoopsaObjectAdapterOptions.None, WoopsaVisibility.All);
            Assert.IsNotNull(adapterA1All.Methods.ByNameOrNull(nameof(ClassA.ToString)));
            IWoopsaObject inner1All = adapterA1All.Items.ByName("Inner1") as IWoopsaObject;
            Assert.IsNotNull(inner1All.Methods.ByNameOrNull(nameof(ClassAInner1.ToString)));



            WoopsaObjectAdapter adapterA2 = new WoopsaObjectAdapter(null, "a", a, WoopsaObjectAdapterOptions.SendTimestamps);
            Assert.IsNotNull(adapterA2.Properties.ByNameOrNull(nameof(a.APropertyBool)).Value.TimeStamp);

            WoopsaObjectAdapter adapterA3 = new WoopsaObjectAdapter(null, "a", a, WoopsaObjectAdapterOptions.DisableClassesCaching);
            Assert.IsNotNull(adapterA1.Properties.ByNameOrNull(nameof(a.APropertyBool)));

            ClassB b = new ClassB();
            WoopsaObjectAdapter adapterB = new WoopsaObjectAdapter(null, "b", b,
                WoopsaObjectAdapterOptions.None, WoopsaVisibility.DefaultIsVisible | WoopsaVisibility.MethodSpecialName |
                WoopsaVisibility.Inherited);
            Assert.IsNotNull(adapterB.Methods.ByNameOrNull("get_" + nameof(ClassB.APropertyBool)));
            Assert.IsNotNull(adapterB.Properties.ByNameOrNull(nameof(b.APropertyBool)));

            ClassC c = new ClassC();
            WoopsaObjectAdapter adapterC = new WoopsaObjectAdapter(null, "c", c);
            Assert.IsNotNull(adapterC.Properties.ByNameOrNull(nameof(c.APropertyBool)));
            Assert.IsNotNull(adapterC.Properties.ByNameOrNull(nameof(c.APropertyTimeSpan)));
            Assert.IsNull(adapterC.Properties.ByNameOrNull("APropertyDouble2"));
            Assert.IsNotNull(adapterC.Properties.ByNameOrNull(nameof(c.APropertyText)));
            IWoopsaProperty propertyText = adapterC.Properties.ByNameOrNull(nameof(c.APropertyText));
            Assert.AreEqual(propertyText.Type, WoopsaValueType.Integer);
            c.APropertyText = "123";
            Assert.AreEqual(propertyText.Value.ToInt64(), 123);
            propertyText.Value = new WoopsaValue(456);
            Assert.AreEqual(c.APropertyText, "456");
            // Json data
            Assert.IsNotNull(adapterC.Properties.ByNameOrNull(nameof(c.APropertyJson)));
            IWoopsaProperty propertyJson = adapterC.Properties.ByNameOrNull(nameof(c.APropertyJson));
            Assert.AreEqual(propertyJson.Type, WoopsaValueType.JsonData);
            // JSon structure
            c.APropertyJson = "{ \"x\" : 8, \"y\": 9 }";
            Assert.IsTrue(propertyJson.Value is WoopsaValue);
            WoopsaValue jsonValue = (WoopsaValue)propertyJson.Value;
            Assert.IsNotNull(jsonValue.JsonData);
            Assert.AreEqual(jsonValue.JsonData["x"].ToInt64(), 8);
            Assert.AreEqual(jsonValue.JsonData["y"].ToInt64(), 9);
            // JSon array
            c.APropertyJson = "{ \"a\" : [11, 12, 13] }";
            Assert.IsTrue(propertyJson.Value is WoopsaValue);
            jsonValue = (WoopsaValue)propertyJson.Value;
            Assert.IsNotNull(jsonValue.JsonData);
            Assert.AreEqual(jsonValue.JsonData["a"][0].ToInt64(), 11);
            Assert.AreEqual(jsonValue.JsonData["a"][1].ToInt64(), 12);
            Assert.AreEqual(jsonValue.JsonData["a"][2].ToInt64(), 13);

            ClassD[] array = new ClassD[] { new ClassD(4), new ClassD(3), new ClassD(2) };
            WoopsaObjectAdapter adapterArrayObject = new WoopsaObjectAdapter(null, "array", array, WoopsaObjectAdapterOptions.None,
                WoopsaVisibility.IEnumerableObject | WoopsaVisibility.DefaultIsVisible);
            Assert.IsNotNull(adapterArrayObject.Items.ByNameOrNull("Item[1]"));
            Assert.IsNotNull(adapterArrayObject.Items.ByNameOrNull("Item[1]") as IWoopsaObject);
            IWoopsaObject item1 = (IWoopsaObject)adapterArrayObject.Items.ByNameOrNull("Item[1]");
            Assert.IsNotNull(item1.Properties.ByNameOrNull(nameof(ClassD.APropertyInt)));
            Assert.AreEqual(item1.Properties.ByNameOrNull(nameof(ClassD.APropertyInt)).Value.ToInt64(), 3);
            item1.Properties.ByNameOrNull(nameof(ClassD.APropertyInt)).Value = new WoopsaValue(5, DateTime.Now);
            Assert.AreEqual(array[1].APropertyInt, 5);
            Assert.AreEqual(item1.Properties.ByNameOrNull(nameof(ClassD.APropertyInt)).Value.ToInt64(), 5);

            int[] dataArray = new int[] { 3, 4, 5 };
            WoopsaObjectAdapter adapterArrayValue = new WoopsaObjectAdapter(null, "array", dataArray, WoopsaObjectAdapterOptions.None,
                WoopsaVisibility.IEnumerableObject | WoopsaVisibility.DefaultIsVisible);
            WoopsaMethod methodGet = adapterArrayValue.Methods.ByNameOrNull("Get");
            Assert.IsNotNull(methodGet);
            int dataItem1 = methodGet.Invoke(1);
            Assert.AreEqual(dataItem1, dataArray[1]);
            WoopsaMethod methodSet = adapterArrayValue.Methods.ByNameOrNull("Set");
            Assert.IsNotNull(methodSet);
            methodSet.Invoke(1, 7);
            dataItem1 = methodGet.Invoke(1);
            Assert.AreEqual(dataArray[1], 7);
            Assert.AreEqual(dataItem1, 7);
        }

    }
}
