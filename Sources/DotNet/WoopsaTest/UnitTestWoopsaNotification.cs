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
    public class UnitTestWoopsaNotification
    {
        #region Consts

        public const int TestingPort = 9999;
        public static string TestingUrl => $"http://localhost:{TestingPort}/woopsa";

        #endregion

        [TestMethod]
        public void TestWoopsaWaitNotification()
        {
            TestObjectServer objectServer = new TestObjectServer();
            using (WoopsaServer server = new WoopsaServer(objectServer, TestingPort))
            {
                using (WoopsaClient client = new WoopsaClient(TestingUrl))
                {
                    WoopsaBoundClientObject root = client.CreateBoundRoot();
                    // Just to show how to see all items
                    foreach (var item in root.Items)
                    {
                        Console.WriteLine("Item = " + item.Name);
                        if (item.Name == "SubscriptionService")
                            Console.WriteLine("Trouvé");
                    }

                    // create a subscription object
                    WoopsaObject subscription = root.Items.ByNameOrNull("SubscriptionService") as WoopsaObject;
                    if (subscription != null)
                    {
                        int result = 0;
                        WoopsaMethod methodCreateScubscriptionChannel = subscription.Methods.ByNameOrNull("CreateSubscriptionChannel");
                        if (methodCreateScubscriptionChannel != null)
                            // call the method "CreateSubscriptionChannel" on the server
                            result = methodCreateScubscriptionChannel.Invoke(1000);   // define the queue size
                        int channel = result;

                        WoopsaMethod methodRegisterScubscription = subscription.Methods.ByNameOrNull("RegisterSubscription");
                        if (methodRegisterScubscription != null)
                            // call the method "registerScubscription" on the server
                            result = methodRegisterScubscription.Invoke(channel, WoopsaValue.WoopsaRelativeLink("/Votes"), 0.01, 0.01);
                        int subscriptionNbr = result;

                        WoopsaJsonData jData;
                        WoopsaMethod methodWaitNotification = subscription.Methods.ByNameOrNull("WaitNotification");
                        if (methodWaitNotification != null)
                        {
                            Stopwatch watch = new Stopwatch();
                            watch.Start();
                            // call the method "WaitNotification" on the server
                            Thread.Sleep(100);
                            jData = methodWaitNotification.Invoke(channel, 0).JsonData;
                            Assert.IsTrue(jData.Length > 0);
                            int lastNotification;
                            lastNotification = jData[0]["Id"];
                            Assert.AreEqual(lastNotification, 1);
                            // Get notifications again
                            Thread.Sleep(100);
                            jData = methodWaitNotification.Invoke(channel, 0).JsonData;
                            Assert.IsTrue(jData.Length > 0);
                            lastNotification = jData[0]["Id"];
                            Assert.AreEqual(lastNotification, 1);
                        }
                    }
                }

            }
        }
    }
}
