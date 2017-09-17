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
        #region Consts

        public const int TestingPort = 9999;
        public static string TestingUrl => $"http://localhost:{TestingPort}/woopsa";

        #endregion

        [TestMethod]
        public void TestWoopsaClientSubscriptionChannel()
        {
            bool isValueChanged = false;
            TestObjectServer objectServer = new TestObjectServer();
            using (WoopsaServer server = new WoopsaServer(objectServer, TestingPort))
            {
                using (WoopsaClient client = new WoopsaClient(TestingUrl))
                {
                    WoopsaBoundClientObject root = client.CreateBoundRoot();
                    WoopsaClientSubscription subscription = root.Subscribe(nameof(TestObjectServer.Votes),
                        (sender, e) => { isValueChanged = true; },
                        TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));
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
        public void TestWoopsaIsLastCommunicationSuccessful()
        {
            bool isSuccessfull = false;
            TestObjectServer objectServer = new TestObjectServer();
            using (WoopsaClient client = new WoopsaClient(TestingUrl))
            {
                WoopsaUnboundClientObject root = client.CreateUnboundRoot("root");
                WoopsaClientSubscription subscription = root.Subscribe(nameof(TestObjectServer.Votes),
                    (sender, e) => {  },
                    TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));
                client.ClientProtocol.IsLastCommunicationSuccessfulChange +=
                    (sender, e) =>
                    {
                        isSuccessfull = client.ClientProtocol.IsLastCommunicationSuccessful;
                    };
                Stopwatch watch = new Stopwatch();
                using (WoopsaServer server = new WoopsaServer(objectServer, TestingPort))
                {                    
                    watch.Restart();
                    while ((!isSuccessfull) && (watch.Elapsed < TimeSpan.FromSeconds(20)))
                        Thread.Sleep(10);
                    if (isSuccessfull)
                        Console.WriteLine("Sucessful after {0} ms", watch.Elapsed.TotalMilliseconds);
                    else
                        Console.WriteLine("No successful communication");
                    Assert.IsTrue(isSuccessfull);
                }
                watch.Restart();
                while ((isSuccessfull) && (watch.Elapsed < TimeSpan.FromSeconds(20)))
                    Thread.Sleep(10);
                if (!isSuccessfull)
                    Console.WriteLine("Communication loss detected after {0} ms", watch.Elapsed.TotalMilliseconds);
                else
                    Console.WriteLine("No communication loss detection");
                Assert.IsFalse(isSuccessfull);
                subscription.Unsubscribe();
            }
        }

        [TestMethod]
        public void TestWoopsaClientSubscriptionToProperty()
        {
            bool isValueChanged = false;
            TestObjectServer objectServer = new TestObjectServer();
            using (WoopsaServer server = new WoopsaServer(objectServer, TestingPort))
            {
                using (WoopsaClient client = new WoopsaClient(TestingUrl))
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

        [TestMethod]
        public void TestWoopsaClientSubscriptionDisappearingProperty()
        {
            bool isValueChanged = false;
            MainClass objectServer = new MainClass();
            InnerClass inner = new InnerClass();
            objectServer.Inner = inner;
            using (WoopsaServer server = new WoopsaServer(objectServer, TestingPort))
            {
                using (WoopsaClient client = new WoopsaClient(TestingUrl))
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

        [TestMethod]
        public void TestWoopsaClientSubscriptionChannel500SubscriptionsList()
        {
            const int objectsCount = 500;
            int totalNotifications = 0;
            List<ManySubscriptionTestObject> list =
                new List<WoopsaTest.UnitTestWoopsaClient.ManySubscriptionTestObject>();
            for (int i = 0; i < objectsCount; i++)
                list.Add(new ManySubscriptionTestObject() { Trigger = i });
            using (WoopsaServer server = new WoopsaServer(new WoopsaObjectAdapter(null, "list", list, null, null,
                WoopsaObjectAdapterOptions.None, WoopsaVisibility.DefaultIsVisible | WoopsaVisibility.IEnumerableObject), TestingPort))
            {
                using (WoopsaClient client = new WoopsaClient(TestingUrl, null, objectsCount))
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
                    while ((totalNotifications < objectsCount) && (watch.Elapsed < TimeSpan.FromSeconds(500)))
                        Thread.Sleep(10);
                    if (totalNotifications == objectsCount)
                        Console.WriteLine("All notification after {0} ms", watch.Elapsed.TotalMilliseconds);
                    else
                        Console.WriteLine("{0} notification received, {1} expected", totalNotifications, objectsCount);
                    Assert.AreEqual(objectsCount, totalNotifications);
                }
            }
        }

        [TestMethod]
        public void TestWoopsaClientSubscriptionChannel5000SubscriptionsObservableCollection()
        {
            const int objectsCount = 5000;
            int totalNotifications = 0;
            ObservableCollection<ManySubscriptionTestObject> list =
                new ObservableCollection<ManySubscriptionTestObject>();
            for (int i = 0; i < objectsCount; i++)
                list.Add(new ManySubscriptionTestObject() { Trigger = i });
            using (WoopsaServer server = new WoopsaServer(new WoopsaObjectAdapter(null, "list", list, null, null,
                WoopsaObjectAdapterOptions.None, WoopsaVisibility.DefaultIsVisible | WoopsaVisibility.IEnumerableObject), TestingPort))
            {
                using (WoopsaClient client = new WoopsaClient(TestingUrl, null, objectsCount))
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
                    while ((totalNotifications < objectsCount) && (watch.Elapsed < TimeSpan.FromSeconds(500)))
                        Thread.Sleep(10);
                    if (totalNotifications == objectsCount)
                        Console.WriteLine("All notification after {0} ms", watch.Elapsed.TotalMilliseconds);
                    else
                        Console.WriteLine("{0} notification received, {1} expected", totalNotifications, objectsCount);
                    Assert.AreEqual(objectsCount, totalNotifications);
                }
            }
        }

        [TestMethod]
        public void TestWoopsaClientSubscriptionChannelServerStartAfter()
        {
            bool isValueChanged = false;
            TestObjectServer objectServer = new TestObjectServer();
            using (WoopsaClient client = new WoopsaClient(TestingUrl))
            {
                WoopsaUnboundClientObject root = client.CreateUnboundRoot("");
                WoopsaClientSubscription subscription = root.Subscribe(nameof(TestObjectServer.Votes),
                    (sender, e) => { isValueChanged = true; },
                    TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));
                using (WoopsaServer server = new WoopsaServer(objectServer, TestingPort))
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
            using (WoopsaClient client = new WoopsaClient(TestingUrl))
            {
                WoopsaUnboundClientObject root = client.CreateUnboundRoot("");
                WoopsaClientSubscription subscription = root.Subscribe(nameof(TestObjectServer.Votes),
                    (sender, e) => { isValueChanged = true; },
                    TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));
                using (WoopsaServer server = new WoopsaServer(objectServer, TestingPort))
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
                using (WoopsaServer server = new WoopsaServer(objectServer, TestingPort))
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
            using (WoopsaClient client = new WoopsaClient(TestingUrl))
            {
                using (WoopsaServer server = new WoopsaServer(objectServer, TestingPort))
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
            int votes = 0;
            WoopsaProperty propertyVotes = new WoopsaProperty(objectServer, "Votes", WoopsaValueType.Integer, (p) => votes,
                (p, value) => { votes = value.ToInt32(); });
            using (WoopsaServer server = new WoopsaServer((IWoopsaContainer)objectServer, TestingPort))
            {
                using (WoopsaClient client = new WoopsaClient(TestingUrl))
                {
                    WoopsaBoundClientObject root = client.CreateBoundRoot();
                    WoopsaClientSubscription subscription = root.Subscribe(nameof(TestObjectServer.Votes),
                        (sender, e) => { isValueChanged = true; },
                        TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));
                    votes = 2;
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
            using (WoopsaServer server = new WoopsaServer(objectServer, TestingPort))
            {
                using (WoopsaClient client = new WoopsaClient(TestingUrl))
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
            using (WoopsaServer server = new WoopsaServer(objectServer, TestingPort))
            {
                using (WoopsaClient client = new WoopsaClient(TestingUrl))
                {
                    int id = client.Root.Subscribe(nameof(TestObjectServer.Votes),
                        (sender, e) => {  },
                        TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));
                    client.Root.Unsubscribe(id);
                    Thread.Sleep(TimeSpan.FromSeconds(40));
                }
            }
        }*/

        #region Inner classes

        public class BaseInnerClass
        {

        }

        public class InnerClass : BaseInnerClass
        {
            public string Info { get; set; }
        }

        public class MainClass
        {
            public BaseInnerClass Inner { get; set; }
        }


        public class ManySubscriptionTestObject
        {
            public int Trigger { get; set; }

            public bool HasNotified { get; set; }
        }

        #endregion
    }
}
