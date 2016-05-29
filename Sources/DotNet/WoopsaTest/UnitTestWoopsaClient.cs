using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
                    int id = client.Root.Subscribe(nameof(TestObjectServer.Votes),
                        (sender, e) => { isValueChanged = true; },
                        TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));
                    objectServer.Votes = 2;
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    while ((!isValueChanged) && (watch.Elapsed < TimeSpan.FromSeconds(2)))
                        Thread.Sleep(10);
                    if (isValueChanged)
                        Console.WriteLine("Notification after {0} ms", watch.Elapsed.TotalMilliseconds);
                    else
                        Console.WriteLine("No notification received");
                    client.Root.Unsubscribe(id);
                    Assert.AreEqual(true, isValueChanged);
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

        [TestMethod]
        public void TestWoopsaClientKeepSubscriptionOpen()
        {
            bool isValueChanged = false;
            TestObjectServer objectServer = new TestObjectServer();
            using (WoopsaClient client = new WoopsaClient("http://localhost/woopsa"))
            {
                using (WoopsaServer server = new WoopsaServer(objectServer))
                {
                    int id = client.Root.Subscribe(nameof(TestObjectServer.Votes),
                        (sender, e) => { isValueChanged = true; },
                        TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));
                    int id2 = client.Root.Subscribe(nameof(TestObjectServer.Votes),
                        (sender, e) => { isValueChanged = true; },
                        TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(100));
                    int id3 = client.Root.Subscribe(nameof(TestObjectServer.Votes),
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
        public void TestWoopsaClientSubscriptionChannelUnexistingItem()
        {            
            TestObjectServer objectServer = new TestObjectServer();
            using (WoopsaServer server = new WoopsaServer(objectServer))
            {
                using (WoopsaClient client = new WoopsaClient("http://localhost/woopsa"))
                {
                    try
                    {
                        int id = client.Root.Subscribe("ThisDoesNotExistInTheServer",
                            (sender, e) => { },
                            TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));
                        Assert.Fail();
                    }
                    catch (Exception e)
                    {

                    }

                }
            }

        }
    }
}
