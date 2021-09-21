using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Woopsa;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

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
        public async Task TestWoopsaClientSubscriptionChannel()
        {
            bool isValueChanged = false;
            TestObjectServer objectServer = new TestObjectServer();
            using WebServer server = new WebServer(objectServer, port: TestingPort);
            server.Start();
            using WoopsaClient client = new WoopsaClient(TestingUrl);

            var tutu = await client.ClientProtocol.MetaAsync();
            var tutu2 = await client.ClientProtocol.MetaAsync();
            WoopsaBoundClientObject root = client.CreateBoundRoot();
            WoopsaClientSubscription subscription = root.Subscribe(nameof(TestObjectServer.Votes),
                (sender, e) => { isValueChanged = true; },
                TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));
            Stopwatch watch = new Stopwatch();
            watch.Start();
            while ((!isValueChanged) && (watch.Elapsed < TimeSpan.FromSeconds(5)))
                Thread.Sleep(1);
            if (isValueChanged)
                Console.WriteLine("Notification after {0} ms", watch.Elapsed.TotalMilliseconds);
            else
                Console.WriteLine("No notification received");
            subscription.Unsubscribe();
            Assert.AreEqual(true, isValueChanged);
        }

        [TestMethod]
        public void TestWoopsaClientSubscriptionChannel2()
        {
            bool isValueChanged = false;
            TestObjectServer objectServer = new TestObjectServer();
            using (WebServer server = new WebServer(objectServer, port:TestingPort))
            {
                server.Start();
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
                    isValueChanged = false;
                    objectServer.Votes = 56;
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
        public async Task TestWoopsaClientSubscriptionChannelAsync()
        {
            TestObjectServer objectServer = new TestObjectServer();
            using (WebServer server = new WebServer(objectServer, port: TestingPort))
            {
                server.Start();
                using (WoopsaClient client = new WoopsaClient(TestingUrl))
                {
                    for (int i = 0; i < 2; i++)
                    {
                        bool isValueChanged = false;
                        bool isValueChanged2 = false;
                        bool isValueChanged3 = false;
                        bool isValueChanged4 = false;
                        bool isValueChanged5 = false;
                        bool isValueChanged6 = false;

                        WoopsaClientSubscription subscription = await client.SubscriptionChannel.SubscribeAsync(nameof(TestObjectServer.Votes),
                        (sender, e) =>
                        {
                            isValueChanged = true;
                        },
                        TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));

                        WoopsaClientSubscription subscription2 = await client.SubscriptionChannel.SubscribeAsync(nameof(TestObjectServer.StringValue),
                            (sender, e) =>
                            {
                                isValueChanged2 = true;
                            },
                            TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));

                        WoopsaClientSubscription subscription3 = await client.SubscriptionChannel.SubscribeAsync(nameof(TestObjectServer.Votes),
                           (sender, e) =>
                           {
                               isValueChanged3 = true;
                           },
                           TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));

                        WoopsaClientSubscription subscription4 = await client.SubscriptionChannel.SubscribeAsync(nameof(TestObjectServer.StringValue),
                            (sender, e) =>
                            {
                                isValueChanged4 = true;
                            },
                            TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));

                        WoopsaClientSubscription subscription5 = await client.SubscriptionChannel.SubscribeAsync(nameof(TestObjectServer.Votes),
                           (sender, e) =>
                           {
                               isValueChanged5 = true;
                           },
                           TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));

                        WoopsaClientSubscription subscription6 = await client.SubscriptionChannel.SubscribeAsync(nameof(TestObjectServer.StringValue),
                            (sender, e) =>
                            {
                                isValueChanged6 = true;
                            },
                            TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));

                        Stopwatch watch = new Stopwatch();
                        watch.Start();
                        while (!(isValueChanged && isValueChanged2 && isValueChanged3 && isValueChanged4 && isValueChanged5 && isValueChanged6) && (watch.Elapsed < TimeSpan.FromSeconds(3)))
                            Thread.Sleep(1);
                        if (isValueChanged)
                            Console.WriteLine("Notification after {0} ms", watch.Elapsed.TotalMilliseconds);
                        else
                            Console.WriteLine("No notification received");

                        if (isValueChanged2)
                            Console.WriteLine("Notification after {0} ms", watch.Elapsed.TotalMilliseconds);
                        else
                            Console.WriteLine("No notification received");

                        if (isValueChanged3)
                            Console.WriteLine("Notification after {0} ms", watch.Elapsed.TotalMilliseconds);
                        else
                            Console.WriteLine("No notification received");

                        if (isValueChanged4)
                            Console.WriteLine("Notification after {0} ms", watch.Elapsed.TotalMilliseconds);
                        else
                            Console.WriteLine("No notification received");

                        if (isValueChanged5)
                            Console.WriteLine("Notification after {0} ms", watch.Elapsed.TotalMilliseconds);
                        else
                            Console.WriteLine("No notification received");

                        if (isValueChanged6)
                            Console.WriteLine("Notification after {0} ms", watch.Elapsed.TotalMilliseconds);
                        else
                            Console.WriteLine("No notification received");

                        subscription.Unsubscribe();
                        subscription2.Unsubscribe();
                        subscription3.Unsubscribe();
                        subscription4.Unsubscribe();
                        subscription5.Unsubscribe();
                        subscription6.Unsubscribe();
                        Assert.AreEqual(true, isValueChanged);
                        Assert.AreEqual(true, isValueChanged2);
                        Assert.AreEqual(true, isValueChanged3);
                        Assert.AreEqual(true, isValueChanged4);
                        Assert.AreEqual(true, isValueChanged5);
                        Assert.AreEqual(true, isValueChanged6);
                    }

                }

            }
        }

        [TestMethod]
        public void TestWoopsaSubscriptionRemovedWhenServerRestart()
        {
            bool isValueChanged = false;
            TestObjectServer objectServer = new TestObjectServer();

            using (WoopsaClient client = new WoopsaClient(TestingUrl))
            {

                using (WebServer server = new WebServer(objectServer, port: TestingPort))
                {
                    server.Start();
                    using (WoopsaBoundClientObject root = client.CreateBoundRoot())
                    {
                        using (WoopsaClientSubscription subscription = root.Subscribe(nameof(TestObjectServer.Votes),
                            (sender, e) => { isValueChanged = true; },
                            TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20)))
                        {
                            Stopwatch watch = new Stopwatch();
                            watch.Start();
                            while ((!isValueChanged) && (watch.Elapsed < TimeSpan.FromSeconds(10)))
                                Thread.Sleep(1);

                            Assert.AreEqual(1, client.SubscriptionChannel.RegisteredSubscriptionCount);
                            if (isValueChanged)
                                Console.WriteLine("Notification after {0} ms", watch.Elapsed.TotalMilliseconds);
                            else
                                Console.WriteLine("No notification received");
                        }
                    }
                    Assert.IsTrue(isValueChanged);
                    Assert.AreEqual(1, client.SubscriptionChannel.SubscriptionsCount);
                    Assert.AreEqual(1, client.SubscriptionChannel.RegisteredSubscriptionCount);
                }

                isValueChanged = false;
                Thread.Sleep(6000);
                using (WebServer server = new WebServer(objectServer, port: TestingPort))
                {
                    server.Start();
                    using (WoopsaBoundClientObject root = client.CreateBoundRoot())
                    {
                        using (WoopsaClientSubscription subscription = root.Subscribe(nameof(TestObjectServer.Votes),
                            (sender, e) => { isValueChanged = true; },
                            TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20)))
                        {
                            Stopwatch watch = new Stopwatch();
                            watch.Start();
                            while ((!isValueChanged) && (watch.Elapsed < TimeSpan.FromSeconds(10)))
                                Thread.Sleep(1);

                            Assert.AreEqual(1, client.SubscriptionChannel.RegisteredSubscriptionCount);
                            if (isValueChanged)
                                Console.WriteLine("Notification after {0} ms", watch.Elapsed.TotalMilliseconds);
                            else
                                Console.WriteLine("No notification received");
                        }
                    }
                    Assert.IsTrue(isValueChanged);
                    Assert.AreEqual(1, client.SubscriptionChannel.SubscriptionsCount);
                    Assert.AreEqual(1, client.SubscriptionChannel.RegisteredSubscriptionCount);
                }
            }
        }

        [TestMethod]
        public void TestWoopsaIsLastCommunicationSuccessful()
        {
            bool isSuccessfull = false;
            int counter = 0;
            TestObjectServer objectServer = new TestObjectServer();
            using (WoopsaClient client = new WoopsaClient(TestingUrl))
            {
                Assert.IsFalse(client.ClientProtocol.IsLastCommunicationSuccessful);

                client.ClientProtocol.IsLastCommunicationSuccessfulChange +=
                (sender, e) =>
                {
                    isSuccessfull = client.ClientProtocol.IsLastCommunicationSuccessful;
                    counter++;
                };
                // Initial & unsuccessful 
                try
                {
                    client.ClientProtocol.Read("Not existing");
                }
                catch
                { }
                Assert.IsFalse(isSuccessfull);
                Assert.AreEqual(1, counter);


                WoopsaUnboundClientObject root = client.CreateUnboundRoot("root");
                WoopsaClientSubscription subscription = root.Subscribe(nameof(TestObjectServer.Votes),
                    (sender, e) => {  },
                    TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));

                Stopwatch watch = new Stopwatch();
                using (WebServer server = new WebServer(objectServer, port: TestingPort))
                {
                    server.Start();
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
            bool isVotesChanged = false;
            bool isStringValueChanged = false;
            TestObjectServer objectServer = new TestObjectServer();
            using (WebServer server = new WebServer(objectServer, port: TestingPort))
            {
                server.Start();
                using (WoopsaClient client = new WoopsaClient(TestingUrl))
                {
                    int newVotes = 0;
                    string newStringValue = string.Empty;
                    WoopsaBoundClientObject root = client.CreateBoundRoot();
                    WoopsaClientProperty propertyVotes = root.Properties.ByName("Votes") as WoopsaClientProperty;
                    WoopsaClientSubscription subscription = propertyVotes.Subscribe((sender, e) => 
                    { 
                        newVotes = e.Notification.Value;
                        isVotesChanged = true; 
                    },
                        TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));

                    WoopsaClientProperty propertyString = root.Properties.ByName("StringValue") as WoopsaClientProperty;
                    WoopsaClientSubscription subscription2 = propertyString.Subscribe((sender, e) => 
                    {
                        var t = client.ClientProtocol.Read("Votes");
                        newStringValue = e.Notification.Value;
                        isStringValueChanged = true; 
                    },
                        TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));

                    objectServer.Votes = 2;
                    objectServer.StringValue = "Test";
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    while ((!isVotesChanged || !isStringValueChanged) && (watch.Elapsed < TimeSpan.FromSeconds(20)))
                        Thread.Sleep(10);
                    if (isVotesChanged)
                        Console.WriteLine("Notification after {0} ms", watch.Elapsed.TotalMilliseconds);
                    else
                        Console.WriteLine("No notification received");
                    subscription.Unsubscribe();
                    Assert.AreEqual(true, isVotesChanged);
                    Assert.AreEqual(true, isStringValueChanged);

                    Assert.AreEqual(2, newVotes);
                    Assert.AreEqual("Test", newStringValue);
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
            using (WebServer server = new WebServer(objectServer, port: TestingPort))
            {
                server.Start();
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
            using (WebServer server = new WebServer(new WoopsaObjectAdapter(null, "list", list, null, null,
                WoopsaObjectAdapterOptions.None, WoopsaVisibility.DefaultIsVisible | WoopsaVisibility.IEnumerableObject), port: TestingPort))
            {
                server.Start();
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
            using (WebServer server = new WebServer(new WoopsaObjectAdapter(null, "list", list, null, null,
                WoopsaObjectAdapterOptions.None, WoopsaVisibility.DefaultIsVisible | WoopsaVisibility.IEnumerableObject), port: TestingPort))
            {
                server.Start();
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
                using (WebServer server = new WebServer(objectServer, port: TestingPort))
                {
                    server.Start();
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
                using (WebServer server = new WebServer(objectServer, port: TestingPort))
                {
                    server.Start();
                    objectServer.Votes = 2;
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    while ((!isValueChanged) && (watch.Elapsed < TimeSpan.FromSeconds(20)))
                        Thread.Sleep(1);
                    if (isValueChanged)
                        Console.WriteLine("Notification after {0} ms", watch.Elapsed.TotalMilliseconds);
                    else
                        Console.WriteLine("No notification received");
                    Assert.AreEqual(true, isValueChanged);
                }
                isValueChanged = false;
                using (WebServer server = new WebServer(objectServer, port: TestingPort))
                {
                    server.Start();
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
                using (WebServer server = new WebServer(objectServer, port: TestingPort))
                {
                    server.Start();
                    WoopsaBoundClientObject root = client.CreateBoundRoot();
                    WoopsaClientSubscription sub = client.SubscriptionChannel.Subscribe(nameof(TestObjectServer.Votes),
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
                    while ((!isValueChanged) && (watch.Elapsed < TimeSpan.FromSeconds(5000)))
                        Thread.Sleep(1);
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
            using (WebServer server = new WebServer((IWoopsaContainer)objectServer, port: TestingPort))
            {
                server.Start();
                using (WoopsaClient client = new WoopsaClient(TestingUrl))
                {
                    WoopsaBoundClientObject root = client.CreateBoundRoot();
                    WoopsaClientSubscription subscription = root.Subscribe(nameof(TestObjectServer.Votes),
                        (sender, e) => { isValueChanged = true; },
                        TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));
                    votes = 2;
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
        public void TestWoopsaClientSubscriptionChannelUnexistingItem()
        {
            TestObjectServer objectServer = new TestObjectServer();
            using (WebServer server = new WebServer(objectServer, port: TestingPort))
            {
                server.Start();
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
