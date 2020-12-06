using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Woopsa;
using System.Collections;
using System.Collections.Generic;

namespace WoopsaTest
{
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
            a.Inner1 = new SubClassAInner1()
            {
                APropertyInt = 123,
                APropertyIntHidden = 0,
                ExtraProperty = 555
            };
            Assert.AreEqual(inner1.Properties.ByName(nameof(SubClassAInner1.ExtraProperty)).Value, 555);

            WoopsaObjectAdapter adapterA1All = new WoopsaObjectAdapter(null, "a", a, null, null,
                WoopsaObjectAdapterOptions.None, WoopsaVisibility.All);
            Assert.IsNotNull(adapterA1All.Methods.ByNameOrNull(nameof(ClassA.ToString)));
            IWoopsaObject inner1All = adapterA1All.Items.ByName("Inner1") as IWoopsaObject;
            Assert.IsNotNull(inner1All.Methods.ByNameOrNull(nameof(ClassAInner1.ToString)));



            WoopsaObjectAdapter adapterA2 = new WoopsaObjectAdapter(null, "a", a, null, null, WoopsaObjectAdapterOptions.SendTimestamps);
            Assert.IsNotNull(adapterA2.Properties.ByNameOrNull(nameof(a.APropertyBool)).Value.TimeStamp);

            WoopsaObjectAdapter adapterA3 = new WoopsaObjectAdapter(null, "a", a, null, null, WoopsaObjectAdapterOptions.None);
            Assert.IsNotNull(adapterA1.Properties.ByNameOrNull(nameof(a.APropertyBool)));

            ClassB b = new ClassB();
            WoopsaObjectAdapter adapterB = new WoopsaObjectAdapter(null, "b", b, null, null,
                WoopsaObjectAdapterOptions.None, WoopsaVisibility.DefaultIsVisible | WoopsaVisibility.MethodSpecialName |
                WoopsaVisibility.Inherited);
            Assert.IsNotNull(adapterB.Methods.ByNameOrNull("get_" + nameof(ClassB.APropertyBool)));
            Assert.IsNotNull(adapterB.Properties.ByNameOrNull(nameof(b.APropertyBool)));

            ClassC c = new ClassC();
            WoopsaObjectAdapter adapterC = new WoopsaObjectAdapter(null, "c", c);
            Assert.IsNull(adapterC.Properties.ByNameOrNull(nameof(c.APropertyBool)));
            Assert.IsNotNull(adapterC.Properties.ByNameOrNull(nameof(c.APropertyTimeSpan)));
            Assert.IsNull(adapterC.Properties.ByNameOrNull("APropertyDouble2"));
            Assert.IsNotNull(adapterC.Properties.ByNameOrNull(nameof(c.APropertyText)));
            IWoopsaProperty propertyText = adapterC.Properties.ByNameOrNull(nameof(c.APropertyText));
            Assert.AreEqual(propertyText.Type, WoopsaValueType.Integer);
            c.APropertyText = "123";
            Assert.AreEqual(propertyText.Value.ToInt64(), 123);
            // Json data
            Assert.IsNotNull(adapterC.Properties.ByNameOrNull(nameof(c.APropertyJson)));
            IWoopsaProperty propertyJson = adapterC.Properties.ByNameOrNull(nameof(c.APropertyJson));
            Assert.AreEqual(propertyJson.Type, WoopsaValueType.JsonData);
            // JSon structure
            c.APropertyJson = "{ \"x\" : 8, \"y\": 9 }";
            Assert.IsTrue(propertyJson.Value is WoopsaValue);
            WoopsaValue jsonValue = (WoopsaValue)propertyJson.Value;
            Assert.IsNotNull(jsonValue.JsonData);
            Assert.AreEqual(jsonValue.JsonData.GetProperty("x").GetInt64(), 8);
            Assert.AreEqual(jsonValue.JsonData.GetProperty("y").GetInt64(), 9);
            // JSon array
            c.APropertyJson = "{ \"a\" : [11, 12, 13] }";
            Assert.IsTrue(propertyJson.Value is WoopsaValue);
            jsonValue = (WoopsaValue)propertyJson.Value;
            Assert.IsNotNull(jsonValue.JsonData);
            Assert.AreEqual(jsonValue.JsonData.GetProperty("a")[0].GetInt64(), 11);
            Assert.AreEqual(jsonValue.JsonData.GetProperty("a")[1].GetInt64(), 12);
            Assert.AreEqual(jsonValue.JsonData.GetProperty("a")[2].GetInt64(), 13);

            ClassD[] array = new ClassD[] { new ClassD(4), new ClassD(3), new ClassD(2) };
            WoopsaObjectAdapter adapterArrayObject = new WoopsaObjectAdapter(null, "array", array, null, null,
                WoopsaObjectAdapterOptions.None,
                WoopsaVisibility.IEnumerableObject | WoopsaVisibility.DefaultIsVisible);
            Assert.IsNotNull(adapterArrayObject.Items.ByNameOrNull(WoopsaObjectAdapter.EnumerableItemDefaultName(1)));
            Assert.IsNotNull(adapterArrayObject.Items.ByNameOrNull(WoopsaObjectAdapter.EnumerableItemDefaultName(1)) as IWoopsaObject);
            IWoopsaObject item1 = (IWoopsaObject)adapterArrayObject.Items.ByNameOrNull(WoopsaObjectAdapter.EnumerableItemDefaultName(1));
            Assert.IsNotNull(item1.Properties.ByNameOrNull(nameof(ClassD.APropertyInt)));
            Assert.AreEqual(item1.Properties.ByNameOrNull(nameof(ClassD.APropertyInt)).Value.ToInt64(), 3);
            item1.Properties.ByNameOrNull(nameof(ClassD.APropertyInt)).Value = new WoopsaValue(5, DateTime.Now);
            Assert.AreEqual(array[1].APropertyInt, 5);
            Assert.AreEqual(item1.Properties.ByNameOrNull(nameof(ClassD.APropertyInt)).Value.ToInt64(), 5);

            int[] dataArray = new int[] { 3, 4, 5 };
            WoopsaObjectAdapter adapterArrayValue = new WoopsaObjectAdapter(null, "array", dataArray, null, null,
                WoopsaObjectAdapterOptions.None,
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
        public void TestWoopsaObjectAdapterEnum()
        {
            ClassE e = new ClassE();
            WoopsaObjectAdapter adapterE = new WoopsaObjectAdapter(null, "e", e, null,
                new WoopsaConverters());
            Assert.IsNotNull(adapterE.Properties.ByNameOrNull(nameof(e.Day)));
            Assert.AreEqual(adapterE.Properties.ByNameOrNull(nameof(e.Day)).Value.Type, WoopsaValueType.Text);
            Assert.AreEqual(adapterE.Properties.ByNameOrNull(nameof(e.Day)).Value.AsText, default(Day).ToString());
            adapterE.Properties.ByNameOrNull(nameof(e.Day)).Value = new WoopsaValue(Day.Thursday.ToString());
            Assert.AreEqual(adapterE.Properties.ByNameOrNull(nameof(e.Day)).Value.AsText, Day.Thursday.ToString());
        }

        [TestMethod]
        public void TestWoopsaObjectAdapterVisibility()
        {
            ClassAInner1 aInner1 = new ClassAInner1();
            // DefaultIsvisible false
            WoopsaObjectAdapter adapterA1 = new WoopsaObjectAdapter(null, "a", aInner1,
                null, null, WoopsaObjectAdapterOptions.None, WoopsaVisibility.None);
            Assert.IsNull(adapterA1.Properties.ByNameOrNull(nameof(aInner1.APropertyInt)));
            Assert.IsNull(adapterA1.Properties.ByNameOrNull(nameof(aInner1.APropertyIntHidden)));
            Assert.IsNotNull(adapterA1.Properties.ByNameOrNull(nameof(aInner1.APropertyIntVisible)));
            Assert.IsNull(adapterA1.Methods.ByNameOrNull(nameof(aInner1.ToString)));
            // DefaultIsvisible true
            WoopsaObjectAdapter adapterA2 = new WoopsaObjectAdapter(null, "a", aInner1,
                null, null, WoopsaObjectAdapterOptions.None, WoopsaVisibility.DefaultIsVisible);
            Assert.IsNotNull(adapterA2.Properties.ByNameOrNull(nameof(aInner1.APropertyInt)));
            Assert.IsNull(adapterA2.Methods.ByNameOrNull(nameof(aInner1.ToString)));
            // DefaultVisibility
            WoopsaObjectAdapter adapterA3 = new WoopsaObjectAdapter(null, "a", aInner1);
            Assert.IsNotNull(adapterA3.Properties.ByNameOrNull(nameof(aInner1.APropertyInt)));
            Assert.IsNull(adapterA3.Properties.ByNameOrNull(nameof(aInner1.APropertyIntHidden)));
            Assert.IsNotNull(adapterA3.Properties.ByNameOrNull(nameof(aInner1.APropertyIntVisible)));
            Assert.IsNull(adapterA3.Methods.ByNameOrNull(nameof(aInner1.ToString)));
            // Visiblity All
            WoopsaObjectAdapter adapterA4 = new WoopsaObjectAdapter(null, "a", aInner1,
                null, null, WoopsaObjectAdapterOptions.None, WoopsaVisibility.All);
            Assert.IsNotNull(adapterA4.Properties.ByNameOrNull(nameof(aInner1.APropertyInt)));
            Assert.IsNull(adapterA4.Properties.ByNameOrNull(nameof(aInner1.APropertyIntHidden)));
            Assert.IsNotNull(adapterA4.Properties.ByNameOrNull(nameof(aInner1.APropertyIntVisible)));
            Assert.IsNotNull(adapterA4.Methods.ByNameOrNull(nameof(aInner1.ToString)));
            // Inherited - no visibility attribute
            SubClassAInner1 aSubInner1 = new SubClassAInner1();
            WoopsaObjectAdapter adapterA5 = new WoopsaObjectAdapter(null, "a", aSubInner1,
                null, null, WoopsaObjectAdapterOptions.None, WoopsaVisibility.None);
            Assert.IsNotNull(adapterA5.Properties.ByNameOrNull(nameof(aSubInner1.ExtraProperty)));
            Assert.IsNull(adapterA5.Properties.ByNameOrNull(nameof(aSubInner1.APropertyIntHidden)));
            Assert.IsNull(adapterA5.Properties.ByNameOrNull(nameof(aSubInner1.APropertyIntVisible)));
            Assert.IsNull(adapterA5.Methods.ByNameOrNull(nameof(aSubInner1.ToString)));
            // Inherited - visibility attribute
            SubClassAInner2 aSubInner2 = new SubClassAInner2();
            WoopsaObjectAdapter adapterA6 = new WoopsaObjectAdapter(null, "a", aSubInner2,
                null, null, WoopsaObjectAdapterOptions.None, WoopsaVisibility.None);
            Assert.IsNotNull(adapterA6.Properties.ByNameOrNull(nameof(aSubInner2.ExtraProperty)));
            Assert.IsNull(adapterA6.Properties.ByNameOrNull(nameof(aSubInner2.APropertyIntHidden)));
            Assert.IsNotNull(adapterA6.Properties.ByNameOrNull(nameof(aSubInner2.APropertyIntVisible)));
            Assert.IsNotNull(adapterA6.Methods.ByNameOrNull(nameof(aSubInner2.ToString)));
            // Inner objects
            ClassA a1 = new ClassA();
            WoopsaObjectAdapter adapterA7 = new WoopsaObjectAdapter(null, "a", a1);
            var a1Inner1 = adapterA7.Items.ByNameOrNull(nameof(a1.Inner1));
            Assert.IsNotNull(a1Inner1);
            var a1Inner1Inner = a1Inner1.Items.ByNameOrNull(nameof(ClassAInner1.Inner)) as WoopsaObjectAdapter;
            Assert.IsNotNull(a1Inner1Inner);
            Assert.IsNotNull(a1Inner1Inner.Properties.ByNameOrNull(
                nameof(a1.Inner1.Inner.APropertyVisible)));
            Assert.IsNotNull(a1Inner1Inner.Properties.ByNameOrNull(
                nameof(a1.Inner1.Inner.APropertyString)));
            Assert.IsNull(a1Inner1Inner.Properties.ByNameOrNull(
                nameof(a1.Inner1.Inner.APropertyHidden)));
            Assert.IsNull(a1Inner1Inner.Methods.ByNameOrNull(
                nameof(a1.Inner1.Inner.ToString)));
            // Update innerObject, retrieve again the adapter and proceeds new Checks
            a1.Inner1 = new SubClassAInner2();
            a1Inner1Inner = a1Inner1.Items.ByNameOrNull(nameof(ClassAInner1.Inner)) as WoopsaObjectAdapter;
            Assert.IsNotNull(a1Inner1Inner.Methods.ByNameOrNull(
                nameof(a1.Inner1.Inner.ToString)));
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

            WoopsaObjectAdapter adapterA1All = new WoopsaObjectAdapter(null, "a", a, null, null,
                WoopsaObjectAdapterOptions.None, WoopsaVisibility.All);
            Assert.IsNotNull(adapterA1All.Methods.ByNameOrNull(nameof(ClassA.ToString)));
            IWoopsaObject inner1All = adapterA1All.Items.ByName("Inner1") as IWoopsaObject;
            Assert.IsNotNull(inner1All.Methods.ByNameOrNull(nameof(ClassAInner1.ToString)));



            WoopsaObjectAdapter adapterA2 = new WoopsaObjectAdapter(null, "a", a, null, null,
                WoopsaObjectAdapterOptions.SendTimestamps);
            Assert.IsNotNull(adapterA2.Properties.ByNameOrNull(nameof(a.APropertyBool)).Value.TimeStamp);

            WoopsaObjectAdapter adapterA3 = new WoopsaObjectAdapter(null, "a", a, null, null,
                WoopsaObjectAdapterOptions.None);
            Assert.IsNotNull(adapterA1.Properties.ByNameOrNull(nameof(a.APropertyBool)));

            ClassB b = new ClassB();
            WoopsaObjectAdapter adapterB = new WoopsaObjectAdapter(null, "b", b, null, null,
                WoopsaObjectAdapterOptions.None, WoopsaVisibility.DefaultIsVisible | WoopsaVisibility.MethodSpecialName |
                WoopsaVisibility.Inherited);
            Assert.IsNotNull(adapterB.Methods.ByNameOrNull("get_" + nameof(ClassB.APropertyBool)));
            Assert.IsNotNull(adapterB.Properties.ByNameOrNull(nameof(b.APropertyBool)));

            ClassC c = new ClassC();
            WoopsaObjectAdapter adapterC = new WoopsaObjectAdapter(null, "c", c);
            Assert.IsNull(adapterC.Properties.ByNameOrNull(nameof(c.APropertyBool)));
            Assert.IsNotNull(adapterC.Properties.ByNameOrNull(nameof(c.APropertyTimeSpan)));
            Assert.IsNull(adapterC.Properties.ByNameOrNull("APropertyDouble2"));
            Assert.IsNotNull(adapterC.Properties.ByNameOrNull(nameof(c.APropertyText)));
            IWoopsaProperty propertyText = adapterC.Properties.ByNameOrNull(nameof(c.APropertyText));
            Assert.AreEqual(propertyText.Type, WoopsaValueType.Integer);
            c.APropertyText = "123";
            Assert.AreEqual(propertyText.Value.ToInt64(), 123);
            // Json data
            Assert.IsNotNull(adapterC.Properties.ByNameOrNull(nameof(c.APropertyJson)));
            IWoopsaProperty propertyJson = adapterC.Properties.ByNameOrNull(nameof(c.APropertyJson));
            Assert.AreEqual(propertyJson.Type, WoopsaValueType.JsonData);
            // JSon structure
            c.APropertyJson = "{ \"x\" : 8, \"y\": 9 }";
            Assert.IsTrue(propertyJson.Value is WoopsaValue);
            WoopsaValue jsonValue = (WoopsaValue)propertyJson.Value;
            Assert.IsNotNull(jsonValue.JsonData);
            Assert.AreEqual(jsonValue.JsonData.GetProperty("x").GetInt64(), 8);
            Assert.AreEqual(jsonValue.JsonData.GetProperty("y").GetInt64(), 9);
            // JSon array
            c.APropertyJson = "{ \"a\" : [11, 12, 13] }";
            Assert.IsTrue(propertyJson.Value is WoopsaValue);
            jsonValue = (WoopsaValue)propertyJson.Value;
            Assert.IsNotNull(jsonValue.JsonData);
            Assert.AreEqual(jsonValue.JsonData.GetProperty("a")[0].GetInt64(), 11);
            Assert.AreEqual(jsonValue.JsonData.GetProperty("a")[1].GetInt64(), 12);
            Assert.AreEqual(jsonValue.JsonData.GetProperty("a")[2].GetInt64(), 13);

            ClassD[] array = new ClassD[] { new ClassD(4), new ClassD(3), new ClassD(2) };
            WoopsaObjectAdapter adapterArrayObject = new WoopsaObjectAdapter(null, "array", array, null, null,
                WoopsaObjectAdapterOptions.None,
                WoopsaVisibility.IEnumerableObject | WoopsaVisibility.DefaultIsVisible);
            Assert.IsNotNull(adapterArrayObject.Items.ByNameOrNull(WoopsaObjectAdapter.EnumerableItemDefaultName(1)));
            Assert.IsNotNull(adapterArrayObject.Items.ByNameOrNull(WoopsaObjectAdapter.EnumerableItemDefaultName(1)) as IWoopsaObject);
            IWoopsaObject item1 = (IWoopsaObject)adapterArrayObject.Items.ByNameOrNull(WoopsaObjectAdapter.EnumerableItemDefaultName(1));
            Assert.IsNotNull(item1.Properties.ByNameOrNull(nameof(ClassD.APropertyInt)));
            Assert.AreEqual(item1.Properties.ByNameOrNull(nameof(ClassD.APropertyInt)).Value.ToInt64(), 3);
            item1.Properties.ByNameOrNull(nameof(ClassD.APropertyInt)).Value = new WoopsaValue(5, DateTime.Now);
            Assert.AreEqual(array[1].APropertyInt, 5);
            Assert.AreEqual(item1.Properties.ByNameOrNull(nameof(ClassD.APropertyInt)).Value.ToInt64(), 5);

            int[] dataArray = new int[] { 3, 4, 5 };
            WoopsaObjectAdapter adapterArrayValue = new WoopsaObjectAdapter(null, "array", dataArray, null, null,
                WoopsaObjectAdapterOptions.None,
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
        public void TestWoopsaObjectAdapterEnumerable()
        {
            List<ClassA> list = new List<WoopsaTest.ClassA>();
            list.Add(new ClassA());
            list.Add(new ClassA());
            WoopsaObjectAdapter adapterList = new WoopsaObjectAdapter(null, "list",
                list, null, null, WoopsaObjectAdapterOptions.None,
                WoopsaVisibility.IEnumerableObject | WoopsaVisibility.Inherited);
            Assert.AreEqual(adapterList.Items.Count, 2);
            list.Add(new ClassA());
            Assert.AreEqual(adapterList.Items.Count, 3);

            List<ClassA> list2 = new List<WoopsaTest.ClassA>();
            WoopsaObjectAdapter adapterList2 = new WoopsaObjectAdapter(null, "list",
                list2, null, null, WoopsaObjectAdapterOptions.None,
                WoopsaVisibility.DefaultIsVisible | WoopsaVisibility.IEnumerableObject | WoopsaVisibility.Inherited |
                WoopsaVisibility.ListClassMembers);
            Assert.AreEqual(adapterList2.Items.Count, 1); // List "Item" indexer
        }
       
        [TestMethod]
        public void TestWoopsaObjectAdapterInterfaces()
        {
            // Base interface
            ClassInterfaceA a = new ClassInterfaceA();
            // Access through type
            WoopsaObjectAdapter adapterA1 = new WoopsaObjectAdapter(null, "a", a);
            Assert.IsNotNull(adapterA1.Properties.ByNameOrNull(nameof(InterfaceA.A)));
            Assert.IsNotNull(adapterA1.Methods.ByNameOrNull(nameof(InterfaceA.MethodA)));
            // Access through Interface
            WoopsaObjectAdapter adapterA2 = new WoopsaObjectAdapter(null, "a", a, typeof(InterfaceA));
            Assert.IsNotNull(adapterA2.Properties.ByNameOrNull(nameof(InterfaceA.A)));
            Assert.IsNotNull(adapterA2.Methods.ByNameOrNull(nameof(InterfaceA.MethodA)));
            // Derived interface
            ClassInterfaceB b = new ClassInterfaceB();
            // Access through type
            WoopsaObjectAdapter adapterB1 = new WoopsaObjectAdapter(null, "b", b);
            Assert.IsNotNull(adapterB1.Properties.ByNameOrNull(nameof(InterfaceA.A)));
            Assert.IsNotNull(adapterB1.Methods.ByNameOrNull(nameof(InterfaceA.MethodA)));
            // Access through Interface
            WoopsaObjectAdapter adapterB2 = new WoopsaObjectAdapter(null, "b", b, typeof(InterfaceB));
            Assert.IsNotNull(adapterB2.Properties.ByNameOrNull(nameof(InterfaceA.A)));
            Assert.IsNotNull(adapterB2.Methods.ByNameOrNull(nameof(InterfaceA.MethodA)));
        }
    }
}
