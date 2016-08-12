using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Woopsa;
using System.Diagnostics;

namespace WoopsaTest
{
    [TestClass]
    public class UnitTestWoopsaClient
    {
        [TestMethod]
        public void TestWoopsaClientSubscriptionChannel()
        {
            bool isValueChanged = false;
            TestObjectServer objectServer = new TestObjectServer();
            using (WoopsaServer server = new WoopsaServer(objectServer))
            {
                using (WoopsaClient client = new WoopsaClient("http://localhost/woopsa"))
                {
                    WoopsaBoundClientObject root = client.CreateBoundRoot();
                    WoopsaClientSubscription subscription = root.Subscribe(nameof(TestObjectServer.Votes),
                        (sender, e) => { isValueChanged = true; },
                        TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));
                    objectServer.Votes = 2;
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    while ((!isValueChanged) && (watch.Elapsed < TimeSpan.FromSeconds(2))) // TODO : 2 s
                        Thread.Sleep(10);
                    if (isValueChanged)
                        Console.WriteLine("Notification after {0} ms", watch.Elapsed.TotalMilliseconds);
                    else
                        Console.WriteLine("No notification received");
                    subscription.Unsubscribe();
                    Assert.AreEqual(true, isValueChanged);
                }
            }
        }

        [TestMethod]
        public void TestWoopsaClientSubscriptionChannelServerStartAfter()
        {
            bool isValueChanged = false;
            TestObjectServer objectServer = new TestObjectServer();
            using (WoopsaClient client = new WoopsaClient("http://localhost/woopsa"))
            {
                WoopsaUnboundClientObject root = client.CreateUnboundRoot("");
                WoopsaClientSubscription subscription = root.Subscribe(nameof(TestObjectServer.Votes),
                    (sender, e) => { isValueChanged = true; },
                    TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));
                using (WoopsaServer server = new WoopsaServer(objectServer))
                {
                    objectServer.Votes = 2;
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    while ((!isValueChanged) && (watch.Elapsed < TimeSpan.FromSeconds(2)))
                        Thread.Sleep(10);
                    if (isValueChanged)
                        Console.WriteLine("Notification after {0} ms", watch.Elapsed.TotalMilliseconds);
                    else
                        Console.WriteLine("No notification received");
                    subscription.Unsubscribe();
                    Assert.AreEqual(true, isValueChanged);
                }
            }
        }

        [TestMethod]
        public void TestWoopsaClientKeepSubscriptionOpen()
        {
            bool isValueChanged = false;
            TestObjectServer objectServer = new TestObjectServer();
            using (WoopsaClient client = new WoopsaClient("http://localhost/woopsa"))
            {
                using (WoopsaServer server = new WoopsaServer(objectServer))
                {
                    WoopsaBoundClientObject root = client.CreateBoundRoot();
                    WoopsaClientSubscription sub = root.Subscribe(nameof(TestObjectServer.Votes),
                        (sender, e) => { isValueChanged = true; },
                        TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));
                    WoopsaClientSubscription sub2 = root.Subscribe(nameof(TestObjectServer.Votes),
                        (sender, e) => { isValueChanged = true; },
                        TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(100));
                    WoopsaClientSubscription sub3 = root.Subscribe(nameof(TestObjectServer.Votes),
                        (sender, e) => { isValueChanged = true; },
                        TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200));
                    objectServer.Votes = 2;
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    while ((!isValueChanged) && (watch.Elapsed < TimeSpan.FromSeconds(2)))
                        Thread.Sleep(10);
                    if (isValueChanged)
                        Console.WriteLine("Notification after {0} ms", watch.Elapsed.TotalMilliseconds);
                    else
                        Console.WriteLine("No notification received");
                    Assert.AreEqual(true, isValueChanged);
                }
            }
        }

        [TestMethod]
        public void TestWoopsaClientSubscriptionChannelNoRemoteSubscriptionService()
        {
            bool isValueChanged = false;
            WoopsaObject objectServer = new WoopsaObject(null, "");
            int Votes =0;
            WoopsaProperty propertyVotes = new WoopsaProperty(objectServer, "Votes", WoopsaValueType.Integer, (p) => Votes,
                (p, value) => { Votes = value.ToInt32(); });
            using (WoopsaServer server = new WoopsaServer((IWoopsaContainer)objectServer))
            {
                using (WoopsaClient client = new WoopsaClient("http://localhost/woopsa"))
                {
                    WoopsaBoundClientObject root = client.CreateBoundRoot();
                    WoopsaClientSubscription subscription = root.Subscribe(nameof(TestObjectServer.Votes),
                        (sender, e) => { isValueChanged = true; },
                        TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));
                    Votes = 2;
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    while ((!isValueChanged) && (watch.Elapsed < TimeSpan.FromSeconds(2000)))
                        Thread.Sleep(10);
                    if (isValueChanged)
                        Console.WriteLine("Notification after {0} ms", watch.Elapsed.TotalMilliseconds);
                    else
                        Console.WriteLine("No notification received");
                    subscription.Unsubscribe();
                    Assert.AreEqual(true, isValueChanged);
                }
            }
        }


        [TestMethod]
        public void TestWoopsaClientSubscriptionChannelUnexistingItem()
        {
            TestObjectServer objectServer = new TestObjectServer();
            using (WoopsaServer server = new WoopsaServer(objectServer))
            {
                using (WoopsaClient client = new WoopsaClient("http://localhost/woopsa"))
                {
                    WoopsaBoundClientObject root = client.CreateBoundRoot();
                    try
                    {
                        WoopsaClientSubscription sub = root.Subscribe("ThisDoesNotExistInTheServer",
                            (sender, e) => { },
                            TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));
                        Assert.Fail();
                    }
                    catch (Exception)
                    {
                    }

                }
            }

        }


        /*        [TestMethod]
        public void TestWoopsaClientSubscriptionChannelTimeout()
        {            
            TestObjectServer objectServer = new TestObjectServer();
            using (WoopsaServer server = new WoopsaServer(objectServer))
            {
                using (WoopsaClient client = new WoopsaClient("http://localhost/woopsa"))
                {
                    int id = client.Root.Subscribe(nameof(TestObjectServer.Votes),
                        (sender, e) => {  },
                        TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));
                    client.Root.Unsubscribe(id);
                    Thread.Sleep(TimeSpan.FromSeconds(40));
                }
            }
        }*/


    }
}
