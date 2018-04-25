using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Woopsa;

namespace WoopsaTest
{
    [TestClass]
    public class UnitTestReflection
    {        

        [TestMethod]
        public void TestWoopsaReflectionIsWoopsaType()
        {
            Assert.IsTrue(WoopsaReflection.IsWoopsaValueType(null, typeof(bool)));
            Assert.IsTrue(WoopsaReflection.IsWoopsaValueType(null, typeof(int)));
            Assert.IsTrue(WoopsaReflection.IsWoopsaValueType(null, typeof(double)));
            Assert.IsTrue(WoopsaReflection.IsWoopsaValueType(null, typeof(DateTime)));
            Assert.IsTrue(WoopsaReflection.IsWoopsaValueType(null, typeof(TimeSpan)));
            Assert.IsTrue(WoopsaReflection.IsWoopsaValueType(null, typeof(string)));
            Assert.IsTrue(WoopsaReflection.IsWoopsaValueType(null, typeof(TypeCode)));
            Assert.IsFalse(WoopsaReflection.IsWoopsaValueType(null, typeof(List<int>)));
        }
    }
}
