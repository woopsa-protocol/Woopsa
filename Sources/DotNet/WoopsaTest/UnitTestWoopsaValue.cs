using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Woopsa;

namespace WoopsaTest
{
	[TestClass]
	public class UnitTestWoopsaValue
	{
		[TestMethod]
		public void TestMethodWoopsaValueLogicalTrue()
		{
			WoopsaValue v1 = true;
			WoopsaValue v2 = true;
			bool b1 = v1;
			Assert.AreEqual(v1.Type, WoopsaValueType.Logical);
			Assert.AreEqual(v1.AsText, WoopsaConst.WoopsaTrue);
			Assert.AreEqual(v1, v2);
			Assert.AreEqual(v1, true);
			Assert.AreEqual(b1, true);
		}

		[TestMethod]
		public void TestMethodWoopsaValueLogicalFalse()
		{
			WoopsaValue v1 = false;
			WoopsaValue v2 = false;
			bool b1 = v1;
			Assert.AreEqual(v1.Type, WoopsaValueType.Logical);
			Assert.AreEqual(v1.AsText, WoopsaConst.WoopsaFalse);
			Assert.AreEqual(v1, v2);
			Assert.AreEqual(v1, false);
			Assert.AreEqual(b1, false);
		}

		[TestMethod]
		public void TestMethodWoopsaValueInteger()
		{
			WoopsaValue v1 = 123;
			WoopsaValue v2 = 123;
			int i1 = v1;
			Assert.AreEqual(v1.Type, WoopsaValueType.Integer);
			Assert.AreEqual(v1.AsText, "123");
			Assert.AreEqual(v1, v2);
			Assert.AreEqual(v1, 123);
			Assert.AreEqual(i1, 123);
		}

		[TestMethod]
		public void TestMethodWoopsaDateTimeTimeSpan()
		{
			WoopsaValue v1 = new DateTime(1972, 11, 1, 10, 11, 12, 13, DateTimeKind.Utc);
			DateTime t1 = v1;
			WoopsaValue v2 = TimeSpan.FromSeconds(1.234);
			TimeSpan t2 = v2;
			Assert.AreEqual(v1.Type, WoopsaValueType.DateTime);
			Assert.IsTrue(v1==t1);
			Assert.IsTrue(t1 == v1);
			Assert.AreEqual(v2.Type, WoopsaValueType.TimeSpan);
			Assert.AreEqual(v1.ToDateTime(), t1);
			Assert.IsTrue(v2== t2);
			Assert.IsTrue(t2 == v2);
			Assert.AreEqual(v2.AsText, "1.234");
		}

		[TestMethod]
		public void TestMethodWoopsaValueExtended()
		{
			string woopsaServer, woopsaItemPath;
			WoopsaValue v = WoopsaValue.WoopsaAbsoluteLink("http://woopsa.demo.org/", "/tunnel1/luftung");
			v.DecodeWoopsaLink(out woopsaServer, out woopsaItemPath);
			Assert.AreEqual(woopsaServer, "http://woopsa.demo.org");
			Assert.AreEqual(woopsaItemPath, "tunnel1/luftung");
			Assert.AreEqual(v.Type, WoopsaValueType.WoopsaLink);
			v = WoopsaValue.WoopsaRelativeLink("/tunnel2/luftung");
			v.DecodeWoopsaLink(out woopsaServer, out woopsaItemPath);
			Assert.AreEqual(woopsaServer, null);
			Assert.AreEqual(woopsaItemPath, "tunnel2/luftung");
			Assert.AreEqual(v.Type, WoopsaValueType.WoopsaLink);
			v = WoopsaValue.WoopsaResourceUrl("http://www.woopsa.org/logo.png");
			Assert.AreEqual(v.AsText, "http://www.woopsa.org/logo.png");
			Assert.AreEqual(v.Type, WoopsaValueType.ResourceUrl);
			v = WoopsaValue.WoopsaJsonData("{\"Name\":\"Switzerland\" , \"Year\":1291}");
			Assert.AreEqual(v.AsText, "{\"Name\":\"Switzerland\" , \"Year\":1291}");
			Assert.AreEqual(v.Type, WoopsaValueType.JsonData);
		}

		[TestMethod]
		public void TestMethodWoopsaValuePerfo()
		{
			Stopwatch watch = new Stopwatch();
			watch.Start();
			for (int i = 0; i < 10000; i++)
			{
				WoopsaValue value = new WoopsaValue(3.14);
			}
			watch.Stop();
			Assert.AreEqual(watch.Elapsed < TimeSpan.FromMilliseconds(500), true);
		}

	}
}
