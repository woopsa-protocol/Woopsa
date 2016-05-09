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

    }
}
