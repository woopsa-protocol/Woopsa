using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Woopsa;
using System.Diagnostics;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace WoopsaTest
{
    [TestClass]
    public class UnitTestWoopsaClient
    {
        [TestMethod]
        public void TestWoopsaClientSubscriptionChannel()
        {
            //Use WoopsaTestServer because uwp doesn't yet implements the server
            bool isValueChanged = false;
            using (WoopsaClient client = new WoopsaClient("http://localhost/woopsa"))
            {
                int id = client.Root.Subscribe(nameof(TestObjectServer.Votes),
                    (sender, e) => { isValueChanged = true; },
                    TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));
                Stopwatch watch = new Stopwatch();
                watch.Start();
                while ((!isValueChanged) && (watch.Elapsed < TimeSpan.FromSeconds(2)))
                    Task.Delay(10);
                if (isValueChanged)
                    Debug.WriteLine("Notification after {0} ms", watch.Elapsed.TotalMilliseconds);
                else
                    Debug.WriteLine("No notification received");
                client.Root.Unsubscribe(id);
                Assert.AreEqual(true, isValueChanged);
            }
        }
    }
}
