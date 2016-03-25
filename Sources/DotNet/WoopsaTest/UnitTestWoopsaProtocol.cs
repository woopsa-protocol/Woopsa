using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Woopsa;

namespace WoopsaTest
{
    public class TestObjectServer
    {
        public int Votes { get; set; }
    }


    [TestClass]
    public class UnitTestWoopsaProtocol
    {
        [TestMethod]
        public void TestWoopsaServerPerformance()
        {
            TestObjectServer objectServer = new TestObjectServer();
            using (WoopsaServer server = new WoopsaServer(objectServer))
            {                
                using (dynamic dynamicClient = new WoopsaDynamicClient("http://localhost/woopsa"))
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
                        Assert.AreEqual((int)dynamicClient.Votes, i);
                        i++;
                    }
                    Console.WriteLine("Invocation duration : {0} ms", watch.Elapsed.TotalMilliseconds / 2 / i);
                    // TODO : Votes.Change does not work as Votes is a WoopsaValue does not contain Change !?!
                    //				dynamicClient.Votes.Change += new EventHandler<WoopsaNotificationEventArgs>((o, e) => Console.WriteLine("Value : {0}", e.Value.ToInt32()));
                    //		Thread.Sleep(100);
                    //	dynamicClient.Votes = 15;
                    //			WoopsaClient client = new WoopsaClient("http://localhost/woopsa");
                    //				int votes = client.Properties.ByName("Votes").Value.ToInt32();
                    //				client.Properties.ByName("Votes");
                }
            }
        }
    }
}
