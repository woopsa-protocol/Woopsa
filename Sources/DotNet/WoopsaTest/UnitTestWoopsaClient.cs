using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Woopsa;

namespace WoopsaTest
{
    [TestClass]
    public class UnitTestWoopsaClient
    {
        public const string TemperaturePropertyName = "Temperature";

        [TestMethod]
        public void TestWoopsaClientSubscriptionChannel()
        {
            _isValueChanged = false;
            ThreadPool.QueueUserWorkItem(ThreadTestSubscriptionChannel);

            int count = 0, timeWait = 50;
            while (!_isValueChanged && count < 10000)
            {
                Thread.Sleep(timeWait);
                count += timeWait;
            }

            if (_isValueChanged)
                Console.WriteLine("The value has changed correctly after {0} ms.", count);

            Assert.AreEqual(true, _isValueChanged);
        }

        private void ThreadTestSubscriptionChannel(object state)
        {
            var client = new WoopsaClient("http://demo.woopsa.org/woopsa");
            int id = client.Root.Subscribe(TemperaturePropertyName, PropertyChangedHandler, TimeSpan.FromMilliseconds(10.0), TimeSpan.FromMilliseconds(30.0));
            Thread.Sleep(TimeSpan.FromSeconds(12.0));
            client.Root.Unsubscribe(id);
            client.Dispose();
        }

        private void PropertyChangedHandler(IWoopsaNotification notification)
        {
            _isValueChanged = true;
        }

        private bool _isValueChanged;
    }
}
