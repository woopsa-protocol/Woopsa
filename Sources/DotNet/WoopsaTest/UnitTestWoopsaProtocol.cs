using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Woopsa;
using System.Collections.Specialized;

namespace WoopsaTest
{
    [TestClass]
    public class UnitTestWoopsaProtocol
    {
        #region Consts

        public const int TestingPort = 9999;
        public const int TestingPortSsl = 443;
        public static string TestingUrl => $"http://localhost:{TestingPort}/woopsa";
        public static string TestingUrlSsl => $"https://localhost:{TestingPortSsl}/woopsa";

        #endregion

        [TestMethod]
        public void TestWoopsaProtocolRootContainer()
        {
            WoopsaRoot serverRoot = new WoopsaRoot();
            TestObjectServer objectServer = new TestObjectServer();
            WoopsaObjectAdapter adapter = new WoopsaObjectAdapter(serverRoot, "TestObject", objectServer);
            using (WebServer server = new WebServer(serverRoot, port: TestingPort))
            {
                server.Start();
                using (WoopsaClient client = new WoopsaClient(TestingUrl))
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
            using (WebServer server = new WebServer(objectServer, port:TestingPort))
            {
                server.Start();
                using (WoopsaClient client = new WoopsaClient(TestingUrl))
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
            using (WoopsaClient client = new WoopsaClient(TestingUrl))
            {
                WoopsaUnboundClientObject root = client.CreateUnboundRoot("root");
                WoopsaProperty propertyVote = root.GetProperty("Votes", WoopsaValueType.Integer, false);
                using (WebServer server = new WebServer(objectServer, port: TestingPort))
                {
                    server.Start();
                    propertyVote.Value = new WoopsaValue(123);
                    Assert.AreEqual(objectServer.Votes, 123);
                    var result = propertyVote.Value;
                    Assert.AreEqual(result.ToInt64(), 123);
                }
            }
        }

        [TestMethod, TestCategory("Performance")]
        public void TestWoopsaProtocolPerformance()
        {
            TestObjectServer objectServer = new TestObjectServer();
            using (WebServer server = new WebServer(objectServer, port: TestingPort))
            {
                server.Start();
                using (WoopsaClient client = new WoopsaClient(TestingUrl))
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
                    Assert.IsTrue(duration < TimeSpan.FromMilliseconds(200), $"Duration takes ${duration.Milliseconds}ms, instead of 200ms");
                    Console.WriteLine($"Duration takes ${duration.Milliseconds}ms");
                }
            }
        }

        private void PropertyChanged(object sender, WoopsaNotificationEventArgs woopsaNotificationEventArgs)
        {
            value = woopsaNotificationEventArgs.Notification.Value;
        }
        string value;

        //[TestMethod, TestCategory("Performance")]
        //public void TestWoopsaProtocolAsyncPerformance()
        //{
        //    TestObjectServer objectServer = new TestObjectServer();
        //    using (WebServer server = new WebServer(objectServer, port: TestingPort, enableSsl: false))
        //    {
        //        server.Start();
        //        using (WoopsaClient client = new WoopsaClient(TestingUrl))
        //        {
        //            client.SubscriptionChannel.Subscribe("StringValue", PropertyChanged);

        //            Stopwatch watch = new Stopwatch();
        //            watch.Start();

        //            for (int i = 0; i < 100; i++)
        //            {
        //                objectServer.StringValue = i.ToString();
        //                Assert.AreEqual(objectServer.StringValue, i.ToString());
        //                while (objectServer.StringValue != value)
        //                {
        //                    Thread.Sleep(1);
        //                }
        //            }
        //            TimeSpan duration = watch.Elapsed;
        //            Assert.IsTrue(duration < TimeSpan.FromSeconds(20), $"Duration takes ${duration.Seconds}s.{duration.Milliseconds}, instead of 200ms");
        //            Console.WriteLine($"Duration takes ${duration.Seconds}s.{duration.Milliseconds}");
        //        }
        //    }
        //}

        //[TestMethod, TestCategory("Performance")]
        //public async Task TestWoopsaProtocolInvocationAsyncPerformance()
        //{
        //    const int numberOfTestBefore = 100;
        //    const int numberOfTasks = 1000;
        //    const int numberOfThreads = 20;
        //    TestObjectServer objectServer = new TestObjectServer();
        //    using (WebServer server = new WebServer(objectServer, port: TestingPort, enableSsl: false))
        //    {
        //        server.Start();
        //        using (WoopsaClient client = new WoopsaClient(TestingUrl))
        //        {
        //            async Task TestAsync()
        //            {
        //                Task<WoopsaValue>[] tasks = new Task<WoopsaValue>[numberOfTasks];
        //                for (int j = 0; j < tasks.Length; j++)
        //                {
        //                    NameValueCollection arguments = new NameValueCollection();
        //                    arguments.Add("count", new WoopsaValue(1));

        //                    Task<WoopsaValue> valueTask = client.ClientProtocol.InvokeAsync(nameof(TestObjectServer.IncrementVotes), arguments);
        //                    tasks[j] = valueTask;
        //                }

        //                for (int j = 0; j < tasks.Length; j++)
        //                {
        //                    await tasks[j];
        //                }
        //            }

        //            void Test()
        //            {
        //                for (int j = 0; j < numberOfTasks; j++)
        //                {
        //                    NameValueCollection arguments = new NameValueCollection();
        //                    arguments.Add("count", new WoopsaValue(1));
        //                    WoopsaValue value = client.ClientProtocol.Invoke(nameof(TestObjectServer.IncrementVotes), arguments);
        //                }
        //            }

        //            objectServer.StringValue = "Test";
        //            // Start up of the server (as it is slower for the first few calls)
        //            for (int i = 0; i < numberOfTestBefore; i++)
        //            {
        //                await TestAsync();
        //            }

        //            // Synchronous calls
        //            Stopwatch watchSync = new Stopwatch();
        //            watchSync.Start();
        //            Thread[] threadsSync = new Thread[numberOfThreads];
        //            for (int i = 0; i < threadsSync.Length; i++)
        //            {
        //                threadsSync[i] = new Thread(() =>
        //                {
        //                    Test();
        //                });
        //                threadsSync[i].Start();
        //            }
        //            for (int i = 0; i < threadsSync.Length; i++)
        //                threadsSync[i].Join(TimeSpan.FromDays(1));
        //            TimeSpan durationSync = watchSync.Elapsed;
        //            Console.WriteLine($"Duration takes ${durationSync.Seconds}s.{durationSync.Milliseconds} for synchronous calls");

        //            // Asynchronous calls
        //            Stopwatch watchAsync = new Stopwatch();
        //            watchAsync.Start();
        //            Thread[] threadsAsync = new Thread[numberOfThreads];
        //            for (int i = 0; i < threadsAsync.Length; i++)
        //            {
        //                threadsAsync[i] = new Thread(() =>
        //                {
        //                    TestAsync().Wait();
        //                });
        //                threadsAsync[i].Start();
        //            }
        //            for (int i = 0; i < threadsAsync.Length; i++)
        //                threadsAsync[i].Join(TimeSpan.FromDays(1));
        //            TimeSpan durationAsync = watchAsync.Elapsed;
        //            Console.WriteLine($"Duration takes ${durationAsync.Seconds}s.{durationAsync.Milliseconds} for asynchronous calls");

        //            Assert.IsTrue(true);
        //        }
        //    }
        //}


        [TestMethod, TestCategory("Performance")]
        public void TestWoopsaServerPerformance()
        {
            TestObjectServer objectServer = new TestObjectServer();
            using (WebServer server = new WebServer(objectServer, port: TestingPort))
            {
                server.Start();
                using (dynamic dynamicClient = new WoopsaDynamicClient(TestingUrl))
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
                    //			WoopsaClient client = new WoopsaClient(TestingUrl);
                    //				int votes = client.Properties.ByName("Votes").Value.ToInt32();
                    //				client.Properties.ByName("Votes");
                }
            }
        }

        [TestMethod]
        public void TestWoopsaServerAuthentication()
        {
            TestObjectServerAuthentification objectServer = new TestObjectServerAuthentification();
            using (WebServer server = new WebServer(objectServer, port: TestingPort))
            {
                server.Start();
                server.Authenticator = new SimpleAuthenticator("TestRealm", (sender, e) =>
                {
                    e.IsAuthenticated = e.Username == "woopsa";
                });
                using (WoopsaClient client = new WoopsaClient(TestingUrl))
                {
                    const string TestUserName = "woopsa";
                    client.Username = TestUserName;
                    client.ClientProtocol.Write("Votes", 5.ToString());
                    Assert.AreEqual(objectServer.Votes, 5);
                    client.Username = "invalid";
                    bool authenticationCheckOk;
                    try
                    {
                        client.ClientProtocol.Write("Votes", 5.ToString());
                        authenticationCheckOk = false;
                    }
                    catch
                    {
                        authenticationCheckOk = true;
                    }
                    Assert.IsTrue(authenticationCheckOk);
                }
            }
        }
    }
}
