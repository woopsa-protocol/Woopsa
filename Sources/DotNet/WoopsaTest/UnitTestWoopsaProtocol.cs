using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Woopsa;
using System.Collections.Specialized;

namespace WoopsaTest
{
    public class TestObjectServer
    {
        public int Votes { get; set; }

        public void IncrementVotes(int count)
        {
            Votes += count;
        }
    }



    [TestClass]
    public class UnitTestWoopsaProtocol
    {

        [TestMethod]
        public void TestWoopsaProtocolRootContainer()
        {
            WoopsaRoot serverRoot = new WoopsaRoot();
            TestObjectServer objectServer = new TestObjectServer();
            WoopsaObjectAdapter adapter = new WoopsaObjectAdapter(serverRoot, "TestObject", objectServer);
            using (WoopsaServer server = new WoopsaServer(serverRoot))
            {
                using (WoopsaClient client = new WoopsaClient("http://localhost/woopsa"))
                {
                    WoopsaBoundClientObject root = client.CreateBoundRoot();
                    (root.Items.ByName("TestObject") as WoopsaObject).Properties.ByName("Votes").Value = 17;
                    Assert.AreEqual(objectServer.Votes, 17);
                }
            }
        }

        [TestMethod]
        public void TestWoopsaProtocol()
        {
            TestObjectServer objectServer = new TestObjectServer();
            using (WoopsaServer server = new WoopsaServer(objectServer))
            {
                using (WoopsaClient client = new WoopsaClient("http://localhost/woopsa"))
                {
                    WoopsaBoundClientObject root = client.CreateBoundRoot();
                    root.Properties.ByName("Votes").Value = new WoopsaValue(11);
                    Assert.AreEqual(objectServer.Votes, 11);
                    var result = root.Properties.ByName("Votes").Value;
                    Assert.AreEqual(11, result.ToInt64());
                    result = root.Methods.ByName(nameof(TestObjectServer.IncrementVotes)).
                        Invoke(5);
                    Assert.AreEqual(16, root.Properties.ByName("Votes").Value.ToInt64());
                    Assert.AreEqual(WoopsaValueType.Null, result.Type);
                    NameValueCollection args = new NameValueCollection();
                    args.Add("count", "8");
                    result = client.ClientProtocol.Invoke("/" + nameof(TestObjectServer.IncrementVotes),
                        args);
                    Assert.AreEqual(24, root.Properties.ByName("Votes").Value.ToInt64());
                    Assert.AreEqual(WoopsaValueType.Null, result.Type);
                }
            }
        }

        [TestMethod]
        public void TestWoopsaProtocolUnboundClient()
        {
            TestObjectServer objectServer = new TestObjectServer();
            using (WoopsaClient client = new WoopsaClient("http://localhost/woopsa"))
            {
                WoopsaUnboundClientObject root = client.CreateUnboundRoot("root");
                WoopsaProperty propertyVote = root.GetProperty("Votes", WoopsaValueType.Integer, false);
                using (WoopsaServer server = new WoopsaServer(objectServer))
                {
                    propertyVote.Value = new WoopsaValue(123);
                    Assert.AreEqual(objectServer.Votes, 123);
                    var result = propertyVote.Value;
                    Assert.AreEqual(result.ToInt64(), 123);
                }
            }
        }

        [TestMethod]
        public void TestWoopsaProtocolPerformance()
        {
            TestObjectServer objectServer = new TestObjectServer();
            using (WoopsaServer server = new WoopsaServer(objectServer))
            {
                using (WoopsaClient client = new WoopsaClient("http://localhost/woopsa"))
                {
                    WoopsaBoundClientObject root = client.CreateBoundRoot();
                    IWoopsaProperty property = root.Properties.ByName("Votes");
                    property.Value = new WoopsaValue(0);
                    int n = property.Value.ToInt32();
                    Stopwatch watch = new Stopwatch();
                    watch.Start();

                    for (int i = 0; i < 100; i++)
                    {
                        property.Value = new WoopsaValue(i);
                        Assert.AreEqual(objectServer.Votes, i);
                        var result = property.Value;
                        Assert.AreEqual(result.ToInt64(), i);
                    }
                    TimeSpan duration = watch.Elapsed;
                    Assert.IsTrue(duration < TimeSpan.FromMilliseconds(200));
                }
            }
        }

        [TestMethod]
        public void TestWoopsaServerPerformance()
        {
            TestObjectServer objectServer = new TestObjectServer();
            using (WoopsaServer server = new WoopsaServer(objectServer))
            {
                using (dynamic dynamicClient = new WoopsaDynamicClient("http://localhost/woopsa"))
                {
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    dynamicClient.Votes = 0;
                    Console.WriteLine("First invocation duration : {0} ms", watch.Elapsed.TotalMilliseconds);
                    int i = 0;
                    watch.Restart();
                    while (watch.Elapsed < TimeSpan.FromSeconds(1))
                    {
                        dynamicClient.Votes = i;
                        Assert.AreEqual(objectServer.Votes, i);
                        Assert.AreEqual((int)dynamicClient.Votes, i);
                        i++;
                    }
                    Console.WriteLine("Invocation duration : {0} ms", watch.Elapsed.TotalMilliseconds / 2 / i);
                    // TODO : Votes.Change does not work as Votes is a WoopsaValue does not contain Change !?!
                    //				dynamicClient.Votes.Change += new EventHandler<WoopsaNotificationEventArgs>((o, e) => Console.WriteLine("Value : {0}", e.Value.ToInt32()));
                    //		Thread.Sleep(100);
                    //	dynamicClient.Votes = 15;
                    //			WoopsaClient client = new WoopsaClient("http://localhost/woopsa");
                    //				int votes = client.Properties.ByName("Votes").Value.ToInt32();
                    //				client.Properties.ByName("Votes");
                }
            }
        }
    }
}
