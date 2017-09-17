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
		public void TestWoopsaValueLogicalTrue()
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
		public void TestWoopsaValueLogicalFalse()
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
		public void TestWoopsaValueInteger()
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
		public void TestWoopsaValueDateTimeTimeSpan()
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
		public void TestWoopsaValueExtended()
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
            Assert.IsNotNull(v.JsonData);
            Assert.AreEqual(v.JsonData["Name"].ToString(), "Switzerland");
            Assert.AreEqual(v.JsonData["Year"].ToInt16(), 1291);
            Assert.AreEqual(v.JsonData["Year"].ToInt32(), 1291);
            Assert.AreEqual(v.JsonData["Year"].ToInt64(), 1291);
            Assert.AreEqual(v.Type, WoopsaValueType.JsonData);
		}

		[TestMethod]
		public void TestWoopsaValuePerfo()
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

        [TestMethod]
        public void TestWoopsaValueCreateChecked()
        {
            WoopsaValue dateTime = WoopsaValue.CreateChecked("2015-03-23T14:15:01Z", WoopsaValueType.DateTime);
            Assert.AreEqual(dateTime.ToDateTime(), new DateTime(2015, 3, 23, 14, 15, 1, DateTimeKind.Utc));
            WoopsaValue integer = WoopsaValue.CreateChecked("123", WoopsaValueType.Integer);
            Assert.AreEqual(integer.ToInt64(), 123);
            WoopsaValue jsonData = WoopsaValue.CreateChecked("{ \"x\": 123 }", WoopsaValueType.JsonData);
            WoopsaValue logical = WoopsaValue.CreateChecked("true", WoopsaValueType.Logical);
            Assert.AreEqual(logical.ToBool(), true);
            WoopsaValue woopsaNull = WoopsaValue.CreateChecked(null, WoopsaValueType.Null);
            Assert.IsTrue(woopsaNull.IsNull());
            WoopsaValue real = WoopsaValue.CreateChecked("1.25", WoopsaValueType.Real);
            Assert.AreEqual(real.ToDouble(), 1.25);
            Assert.IsFalse(real.IsNull());
            WoopsaValue resourceUrl = WoopsaValue.CreateChecked("http://www.woopsa.org/logo.png", 
                WoopsaValueType.ResourceUrl);
            const string sentence = "Don't worry, be happy";
            WoopsaValue text = WoopsaValue.CreateChecked(sentence, WoopsaValueType.Text);
            Assert.AreEqual(text.AsText, sentence);
            Assert.AreEqual(text.ToString(), sentence);
            WoopsaValue timeSpan = WoopsaValue.CreateChecked("0.001", WoopsaValueType.TimeSpan);
            Assert.AreEqual(timeSpan.ToTimeSpan(), TimeSpan.FromMilliseconds(1));
            WoopsaValue timeSpan2 = WoopsaValue.CreateChecked("3600", WoopsaValueType.TimeSpan);
            Assert.AreEqual(timeSpan2.ToTimeSpan(), TimeSpan.FromHours(1));
            WoopsaValue woopsaLink = WoopsaValue.CreateChecked("http://demo.woopsa.org/weather", 
                WoopsaValueType.WoopsaLink);
        }


        [TestMethod]
        public void TestWoopsaSerializationPerfo()
        {
            Stopwatch watch = new Stopwatch();
            WoopsaValue v = 3.14;
            watch.Start();
            for (int i = 0; i < 1000000; i++)
            {
                v.Serialize();
            }
            watch.Stop();
            Assert.AreEqual(watch.Elapsed < TimeSpan.FromMilliseconds(1000), true);
        }


    }
}
