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
    public class UnitTestWoopsaDynamicNotification
    {
        const int QUEUE_SIZE = 1000;
        const int MONITOR_INTERVAL = 10;
        const int PUBLISH_INTERVAL = 10;

        [TestMethod]
        public void TestWoopsaDynamicNotification()
        {
            TestObjectServer objectServer = new TestObjectServer();
            using (WoopsaServer server = new WoopsaServer(objectServer))
            {
                // Solution with dynamic client
                using (dynamic dynamicClient = new WoopsaDynamicClient("http://localhost/woopsa"))
                {

                    int channel = dynamicClient.SubscriptionService.CreateSubscriptionChannel(QUEUE_SIZE);
                    // Subscription for a valid variable
                    dynamicClient.SubscriptionService.RegisterSubscription(channel,
                       WoopsaValue.WoopsaRelativeLink("/Votes"), TimeSpan.FromMilliseconds(MONITOR_INTERVAL),
                       TimeSpan.FromMilliseconds(PUBLISH_INTERVAL));

                    // Subscription for an nonexistent variable (just for test)
                    try
                    {
                        dynamicClient.SubscriptionService.RegisterSubscription(channel,
                            WoopsaValue.WoopsaRelativeLink("/Vote"), TimeSpan.FromMilliseconds(MONITOR_INTERVAL),
                            TimeSpan.FromMilliseconds(PUBLISH_INTERVAL));
                        Assert.Fail("Nonexistent variable"); 
                    }
                    catch (Exception)
                    {
                        // just catch the exception (normal behaviour)
                    }

                    Stopwatch watch = new Stopwatch();
                    WoopsaValue lastNotifications;
                    WoopsaJsonData jsonData;
                    int lastNotificationId;
                    watch.Start();
                    do
                    {
                        lastNotifications = dynamicClient.SubscriptionService.WaitNotification(channel, 0);
                        Assert.AreEqual(lastNotifications.Type, WoopsaValueType.JsonData);
                        jsonData = lastNotifications.JsonData;
                        if (watch.ElapsedMilliseconds > 1000)
                            Assert.Fail("Timeout without receveiving any notification");
                    }
                    while (jsonData.Length == 0);
                    lastNotificationId = jsonData[jsonData.Length - 1]["Id"];
                    Assert.AreEqual(lastNotificationId, 1);
                    // Get again the same notification
                    lastNotifications = dynamicClient.SubscriptionService.WaitNotification(channel, 0);
                    jsonData = lastNotifications.JsonData;
                    Assert.IsTrue(jsonData.IsArray);
                    Assert.AreEqual(jsonData.Length, 1);
                    Assert.IsTrue(jsonData[0].IsDictionary);
                    lastNotificationId = jsonData[jsonData.Length - 1]["Id"];
                    Assert.AreEqual(lastNotificationId, 1);
                    // Generate a new notification
                    objectServer.Votes++;
                    Thread.Sleep(PUBLISH_INTERVAL * 10);
                    // Check we have now 2
                    lastNotifications = dynamicClient.SubscriptionService.WaitNotification(channel, 0);
                    jsonData = lastNotifications.JsonData;
                    Assert.IsTrue(jsonData.IsArray);
                    Assert.AreEqual(jsonData.Length, 2);
                    Assert.IsTrue(jsonData[0].IsDictionary);
                    lastNotificationId = jsonData[jsonData.Length - 1]["Id"];
                    Assert.AreEqual(lastNotificationId, 2);
                    // Check we can remove 1 and still have 1
                    lastNotifications = dynamicClient.SubscriptionService.WaitNotification(channel, 1);
                    jsonData = lastNotifications.JsonData;
                    Assert.AreEqual(jsonData.Length, 1);
                    lastNotificationId = jsonData[jsonData.Length - 1]["Id"];
                    Assert.AreEqual(lastNotificationId, 2);
                    // Enable the code below to test the wait of the timeout when they are 0 notifications pending
                    /*
                    // Check we can remove 1 and have 0. This takes 5 seconds (we wait the timeout)
                    lastNotifications = dynamicClient.SubscriptionService.WaitNotification(channel, lastNotificationId);
                    jsonData = lastNotifications.JsonData;
                    Assert.AreEqual(jsonData.Length, 0);
                    */
                }
            }
        } //end TestWoopsaDynamicNotification
    }
}
