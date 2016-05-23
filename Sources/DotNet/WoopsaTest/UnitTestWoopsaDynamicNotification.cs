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
        const int MONITOR_INTERVAL = 100;
        const int PUBLISH_INTERVAL = 100;

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
                    catch (Exception e)
                    {
                        // just catch the exception (normal behaviour)
                    }

                    WoopsaValue lastNotifications;
                    WoopsaJsonData jsonData;
                    int lastNotificationId;
                    lastNotifications = dynamicClient.SubscriptionService.WaitNotification(channel, 0);
                    Assert.AreEqual(lastNotifications.Type, WoopsaValueType.JsonData);
                    jsonData = lastNotifications.JsonData;
                    lastNotificationId = jsonData[jsonData.Length - 1]["Id"];
                    Assert.AreEqual(lastNotificationId, 1);
                    //TODO: voir avec FBG si OK: array vide
                    //for (int index = 0; index < 10; index++)
                    //{
                    //    lastNotifications = dynamicClient.SubscriptionService.WaitNotification(channel, lastNotificationId);
                    //    Assert.AreEqual(lastNotifications.Type, WoopsaValueType.JsonData);
                    //    jsonData = lastNotifications.JsonData;
                    //    Assert.AreEqual(lastNotificationId, index + 2);  // first loop with ID of 2
                    //    lastNotificationId = jsonData[jsonData.Length - 1]["Id"];
                    //}
                    lastNotifications = dynamicClient.SubscriptionService.WaitNotification(channel, 0);
                    jsonData = lastNotifications.JsonData;
                    Assert.IsTrue(jsonData.IsArray);
                    //Assert.IsTrue(jsonData[jsonData.Length - 1].IsDictionary);
                    //lastNotificationId = jsonData[jsonData.Length - 1]["Id"];
                    //Assert.AreEqual(lastNotificationId, 1);
                    //lastNotifications = dynamicClient.SubscriptionService.WaitNotification(channel, lastNotificationId);
                    jsonData = lastNotifications.JsonData;
                    Assert.AreEqual(jsonData.Length, 0);
                }
            }
        } //end TestWoopsaDynamicNotification
    }
}
