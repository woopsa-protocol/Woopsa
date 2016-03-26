using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Woopsa;
using System.Collections;

namespace WoopsaTest
{
    [WoopsaVisibility(WoopsaVisibility.DefaultVisible | WoopsaVisibility.Inherited | WoopsaVisibility.Object)]
    public class ClassAInner1
    {        
        public int APropertyInt { get; set; }

        [WoopsaVisible(false)]
        public int APropertyIntHidden { get; set; }

    }

    public class ClassA
    {
        public ClassA()
        {
            Inner1 = new ClassAInner1();
        }
        public bool APropertyBool { get; set; }

        [WoopsaVisible(false)]
        public DateTime APropertyDateTime{ get; set; }
        public DateTime APropertyDateTime2 { get; set; }

        public ClassAInner1 Inner1 { get; private set; }
    }

    [WoopsaVisibility(WoopsaVisibility.DefaultVisible | WoopsaVisibility.Inherited | WoopsaVisibility.MethodSpecialName)]
    public  class ClassB: ClassA
    {
        public int APropertyInt { get; set; }
    }

    [WoopsaVisibility(WoopsaVisibility.None)]
    public class ClassC: ClassB
    {
        double APropertyDouble { get; set; }

        [WoopsaVisible(true)]
       public  TimeSpan APropertyTimeSpan { get; set; }
    }

    public class ClassD 
    {
        public ClassD(int n) { APropertyInt = n;  }
        public int APropertyInt { get;  set; }
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
            Assert.AreEqual(inner1.Properties.ByNameOrNull(nameof(ClassAInner1.APropertyInt)).Value.ToInt64(), 12);
            Assert.IsNotNull(inner1.Methods.ByNameOrNull(nameof(ClassAInner1.ToString)));
           

            WoopsaObjectAdapter adapterA2 = new WoopsaObjectAdapter(null, "a", a, WoopsaObjectAdapterOptions.SendTimestamps);
            Assert.IsNotNull(adapterA2.Properties.ByNameOrNull(nameof(a.APropertyBool)).Value.TimeStamp);

            WoopsaObjectAdapter adapterA3 = new WoopsaObjectAdapter(null, "a", a, WoopsaObjectAdapterOptions.DisableClassesCaching);
            Assert.IsNotNull(adapterA1.Properties.ByNameOrNull(nameof(a.APropertyBool)));

            ClassB b = new ClassB();
            WoopsaObjectAdapter adapterB = new WoopsaObjectAdapter(null, "b", b);
            Assert.IsNotNull(adapterB.Methods.ByNameOrNull("get_" + nameof(ClassB.APropertyBool)));
            Assert.IsNotNull(adapterB.Properties.ByNameOrNull(nameof(b.APropertyBool)));

            ClassC c = new ClassC();
            WoopsaObjectAdapter adapterC = new WoopsaObjectAdapter(null, "c", c);
            Assert.IsNull(adapterC.Properties.ByNameOrNull(nameof(b.APropertyBool)));

            ClassD[] array = new ClassD[] { new ClassD(4), new ClassD(3), new ClassD(2) };
            WoopsaObjectAdapter adapterArray = new WoopsaObjectAdapter(null, " array", array, WoopsaObjectAdapterOptions.None,
                WoopsaVisibility.IEnumerable | WoopsaVisibility.DefaultVisible);
            Assert.IsNotNull(adapterArray.Items.ByNameOrNull("Item[1]"));
            Assert.IsNotNull(adapterArray.Items.ByNameOrNull("Item[1]") as IWoopsaObject);
            IWoopsaObject item1 = (IWoopsaObject)adapterArray.Items.ByNameOrNull("Item[1]");
            Assert.IsNotNull(item1.Properties.ByNameOrNull(nameof(ClassD.APropertyInt)));
            Assert.AreEqual(item1.Properties.ByNameOrNull(nameof(ClassD.APropertyInt)).Value.ToInt64(), 3);
            item1.Properties.ByNameOrNull(nameof(ClassD.APropertyInt)).Value = new WoopsaValue(5, DateTime.Now);
            Assert.AreEqual(array[1].APropertyInt, 5);
            Assert.AreEqual(item1.Properties.ByNameOrNull(nameof(ClassD.APropertyInt)).Value.ToInt64(), 5);
        }
    }
}
