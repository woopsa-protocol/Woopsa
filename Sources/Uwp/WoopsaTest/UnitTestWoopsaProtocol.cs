using System;
using System.Diagnostics;
using System.Threading;
using Woopsa;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace WoopsaTest
{
    public class TestObjectServer
    {
        public int Votes { get; set; }
    }

    [TestClass]
    public class UnitTestWoopsaProtocol
    {

        [TestMethod]
        public void TestWoopsaProtocol()
        {
            throw new NotImplementedException();
            //TestObjectServer objectServer = new TestObjectServer();
            //using (WoopsaServer server = new WoopsaServer(objectServer))
            //{
            //    using (WoopsaClient client = new WoopsaClient("http://localhost/woopsa"))
            //    {
            //        client.Root.Properties.ByName("Votes").Value = new WoopsaValue(11);
            //        Assert.AreEqual(objectServer.Votes, 11);
            //        var result = client.Root.Properties.ByName("Votes").Value;
            //        Assert.AreEqual(result.ToInt64(), 11);
            //    }
            //}
        }

        [TestMethod]
        public void TestWoopsaProtocolPerformance()
        {
            throw new NotImplementedException();
            //TestObjectServer objectServer = new TestObjectServer();
            //using (WoopsaServer server = new WoopsaServer(objectServer))
            //{
            //    using (WoopsaClient client = new WoopsaClient("http://localhost/woopsa"))
            //    {
            //        IWoopsaProperty property = client.Root.Properties.ByName("Votes");
            //        property.Value = new WoopsaValue(0);
            //        int n = property.Value.ToInt32();
            //        Stopwatch watch = new Stopwatch();
            //        watch.Start();

            //        for (int i = 0; i < 100; i++)
            //        {
            //            property.Value = new WoopsaValue(i);
            //            Assert.AreEqual(objectServer.Votes, i);
            //            var result = property.Value;
            //            Assert.AreEqual(result.ToInt64(), i);
            //        }
            //        TimeSpan duration = watch.Elapsed;
            //        Assert.IsTrue(duration < TimeSpan.FromMilliseconds(200));
            //    }
            //}
        }

        [TestMethod]
        public void TestWoopsaServerPerformance()
        {
            throw new NotImplementedException();
            //TestObjectServer objectServer = new TestObjectServer();
            //using (WoopsaServer server = new WoopsaServer(objectServer))
            //{
            //    using (dynamic dynamicClient = new WoopsaDynamicClient("http://localhost/woopsa"))
            //    {
            //        Stopwatch watch = new Stopwatch();
            //        watch.Start();
            //        dynamicClient.Votes = 0;
            //        Debug.WriteLine("First invocation duration : {0} ms", watch.Elapsed.TotalMilliseconds);
            //        int i = 0;
            //        watch.Restart();
            //        while (watch.Elapsed < TimeSpan.FromSeconds(1))
            //        {
            //            dynamicClient.Votes = i;
            //            Assert.AreEqual(objectServer.Votes, i);
            //            Assert.AreEqual((int)dynamicClient.Votes, i);
            //            i++;
            //        }
            //        Debug.WriteLine("Invocation duration : {0} ms", watch.Elapsed.TotalMilliseconds / 2 / i);
            //    }
            //}
        }
    }
}
