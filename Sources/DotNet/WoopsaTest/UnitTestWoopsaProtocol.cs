using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Woopsa;

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
        public void TestWoopsaProtocolRootContainer()
        {
            WoopsaRoot root = new WoopsaRoot();
            TestObjectServer objectServer = new TestObjectServer();
            WoopsaObjectAdapter adapter = new WoopsaObjectAdapter(root, "TestObject", objectServer);
            using (WoopsaServer server = new WoopsaServer(root))
            {
                using (WoopsaClient client = new WoopsaClient("http://localhost/woopsa"))
                {
                    (client.Root.Items.ByName("TestObject") as WoopsaObject).Properties.ByName("Votes").Value = 17;
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
                    client.Root.Properties.ByName("Votes").Value = new WoopsaValue(11);
                    Assert.AreEqual(objectServer.Votes, 11);
                    var result = client.Root.Properties.ByName("Votes").Value;
                    Assert.AreEqual(result.ToInt64(), 11);
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
                    IWoopsaProperty property = client.Root.Properties.ByName("Votes");
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
