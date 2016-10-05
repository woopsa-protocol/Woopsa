using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Woopsa;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
                    while ((!isValueChanged) && (watch.Elapsed < TimeSpan.FromSeconds(20))) 
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
        public void TestWoopsaClientSubscriptionToProperty()
        {
            bool isValueChanged = false;
            TestObjectServer objectServer = new TestObjectServer();
            using (WoopsaServer server = new WoopsaServer(objectServer))
            {
                using (WoopsaClient client = new WoopsaClient("http://localhost/woopsa"))
                {
                    WoopsaBoundClientObject root = client.CreateBoundRoot();
                    WoopsaClientProperty propertyVotes = root.Properties.ByName("Votes") as WoopsaClientProperty;
                    WoopsaClientSubscription subscription = propertyVotes.Subscribe((sender, e) => { isValueChanged = true; },
                        TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));
                    objectServer.Votes = 2;
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    while ((!isValueChanged) && (watch.Elapsed < TimeSpan.FromSeconds(20))) 
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

        public class BaseInnerClass
        {

        }

        public class InnerClass: BaseInnerClass
        {
            public string Info { get; set; }
        }

        public class MainClass
        {            
            public BaseInnerClass Inner { get; set; }
        }

        [TestMethod]
        public void TestWoopsaClientSubscriptionDisappearingProperty()
        {
            bool isValueChanged = false;
            MainClass objectServer = new MainClass();
            InnerClass inner = new InnerClass();
            objectServer.Inner = inner;
            using (WoopsaServer server = new WoopsaServer(objectServer))
            {
                using (WoopsaClient client = new WoopsaClient("http://localhost/woopsa"))
                {
                    WoopsaBoundClientObject root = client.CreateBoundRoot();
                    WoopsaObject Inner = root.Items.ByName(nameof(MainClass.Inner)) as WoopsaObject;
                    WoopsaClientProperty propertyInfo = Inner.Properties.ByName(nameof(InnerClass.Info)) as WoopsaClientProperty;
                    WoopsaClientSubscription subscription = propertyInfo.Subscribe(
                        (sender, e) =>
                        {
                            isValueChanged = true;
                        },
                        TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));
                    inner.Info = "Test";
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    while ((!isValueChanged) && (watch.Elapsed < TimeSpan.FromSeconds(20))) 
                        Thread.Sleep(10);
                    if (isValueChanged)
                        Console.WriteLine("Notification after {0} ms", watch.Elapsed.TotalMilliseconds);
                    else
                        Console.WriteLine("No notification received");
                    isValueChanged = false;
                    objectServer.Inner = new BaseInnerClass();
//                    objectServer.Inner = new object();
                    while ((!isValueChanged) && (watch.Elapsed < TimeSpan.FromSeconds(20))) 
                        Thread.Sleep(10);
                    subscription.Unsubscribe();
                    Assert.AreEqual(true, isValueChanged);
                }
            }
        }



        public class ManySubscriptionTestObject
        {
            public int Trigger { get; set; }

            public bool HasNotified { get; set; }
        }

        [TestMethod]
        public void TestWoopsaClientSubscriptionChannel500SubscriptionsList()
        {
            const int ObjectsCount = 500;
            int totalNotifications = 0;
            List<ManySubscriptionTestObject> list = 
                new List<WoopsaTest.UnitTestWoopsaClient.ManySubscriptionTestObject>();
            for (int i = 0; i < ObjectsCount; i++)
                list.Add(new ManySubscriptionTestObject() { Trigger = i });
            using (WoopsaServer server = new WoopsaServer(new WoopsaObjectAdapter(null, "list", list, null, null,
                WoopsaObjectAdapterOptions.None, WoopsaVisibility.DefaultIsVisible | WoopsaVisibility.IEnumerableObject)))
            {
                using (WoopsaClient client = new WoopsaClient("http://localhost/woopsa", null, ObjectsCount))
                {
                    WoopsaBoundClientObject root = client.CreateBoundRoot();
                    for (int i = 0; i < list.Count; i++)
                    {
                        int index = i;
                        WoopsaClientSubscription subscription = root.Subscribe(
                            WoopsaUtils.CombinePath(
                                WoopsaObjectAdapter.EnumerableItemDefaultName(i), 
                                nameof(ManySubscriptionTestObject.Trigger)),
                            (sender, e) =>
                            {
                                list[index].HasNotified = true;
                                totalNotifications++;
                            },
                            TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(200));
                    }
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    while ((totalNotifications < ObjectsCount) && (watch.Elapsed < TimeSpan.FromSeconds(500)))
                        Thread.Sleep(10);
                    if (totalNotifications == ObjectsCount)
                        Console.WriteLine("All notification after {0} ms", watch.Elapsed.TotalMilliseconds);
                    else
                        Console.WriteLine("{0} notification received, {1} expected", totalNotifications, ObjectsCount);
                    Assert.AreEqual(ObjectsCount, totalNotifications);
                }
            }
        }

        [TestMethod]
        public void TestWoopsaClientSubscriptionChannel5000SubscriptionsObservableCollection()
        {
            const int ObjectsCount = 5000;
            int totalNotifications = 0;
            ObservableCollection<ManySubscriptionTestObject> list =
                new ObservableCollection<WoopsaTest.UnitTestWoopsaClient.ManySubscriptionTestObject>();
            for (int i = 0; i < ObjectsCount; i++)
                list.Add(new ManySubscriptionTestObject() { Trigger = i });
            using (WoopsaServer server = new WoopsaServer(new WoopsaObjectAdapter(null, "list", list, null, null,
                WoopsaObjectAdapterOptions.None, WoopsaVisibility.DefaultIsVisible | WoopsaVisibility.IEnumerableObject)))
            {
                using (WoopsaClient client = new WoopsaClient("http://localhost/woopsa", null, ObjectsCount))
                {
                    WoopsaBoundClientObject root = client.CreateBoundRoot();
                    for (int i = 0; i < list.Count; i++)
                    {
                        int index = i;
                        WoopsaClientSubscription subscription = root.Subscribe(
                            WoopsaUtils.CombinePath(
                                WoopsaObjectAdapter.EnumerableItemDefaultName(i),
                                nameof(ManySubscriptionTestObject.Trigger)),
                            (sender, e) =>
                            {
                                list[index].HasNotified = true;
                                totalNotifications++;
                            },
                            TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(200));
                    }
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    while ((totalNotifications < ObjectsCount) && (watch.Elapsed < TimeSpan.FromSeconds(500)))
                        Thread.Sleep(10);
                    if (totalNotifications == ObjectsCount)
                        Console.WriteLine("All notification after {0} ms", watch.Elapsed.TotalMilliseconds);
                    else
                        Console.WriteLine("{0} notification received, {1} expected", totalNotifications, ObjectsCount);
                    Assert.AreEqual(ObjectsCount, totalNotifications);
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
        public void TestWoopsaClientSubscriptionChannelServerRestartAfter()
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
                    Assert.AreEqual(true, isValueChanged);
                }
                isValueChanged = false;
                using (WoopsaServer server = new WoopsaServer(objectServer))
                {
                    objectServer.Votes = 3;
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
                subscription.Unsubscribe();
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
            int Votes = 0;
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
