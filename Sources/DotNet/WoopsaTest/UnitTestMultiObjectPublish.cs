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
    public class UnitTestMultiObjectPublish
    {
        #region Consts

        public const int TestingPort = 9999;

        public static string Prefix1 = "woopsa1";
        public static string Prefix2 = "woopsa2";

        public static string TestingUrl1 => $"http://localhost:{TestingPort}/{Prefix1}";
        public static string TestingUrl2 => $"http://localhost:{TestingPort}/{Prefix2}";

        #endregion

        [TestMethod]
        public async Task TestMultiObjectPublish()
        {
            bool isValueChanged = false;
            TestObjectServer objectServer = new TestObjectServer();
            using (WebServer server = new WebServer(objectServer, TestingPort, Prefix1))
            {
                TestObjectServer2 objectServer2 = new TestObjectServer2();
                server.AddEndPoint(new EndpointWoopsa(objectServer2, Prefix2));
                server.Start();
                using (WoopsaClient client = new WoopsaClient(TestingUrl1))
                {
                    var test = await client.ClientProtocol.MetaAsync();

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

                using (WoopsaClient client = new WoopsaClient(TestingUrl2))
                {
                    WoopsaBoundClientObject root = client.CreateBoundRoot();
                    WoopsaClientSubscription subscription = root.Subscribe(nameof(TestObjectServer2.Votes),
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
            }
        }
    }
}
