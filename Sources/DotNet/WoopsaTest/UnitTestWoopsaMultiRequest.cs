using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Woopsa;
using System.Collections.Generic;

namespace WoopsaTest
{
    [TestClass]
    public class UnitTestWoopsaMultiRequest
    {
        #region Consts

        public const int TestingPort = 9999;
        public static string TestingUrl => $"http://localhost:{TestingPort}/woopsa";
        
        #endregion

        [TestMethod]
        public void TestWoopsaMultiRequest()
        {
            WoopsaObject serverRoot = new WoopsaObject(null, "");
            TestObjectMultiRequest objectServer = new TestObjectMultiRequest();
            new WoopsaObjectAdapter(serverRoot, "TestObject", objectServer);
            using (WebServer server = new WebServer(serverRoot, TestingPort))
            {
                server.Start();
                using (WoopsaClient client = new WoopsaClient(TestingUrl))
                {
                    ExecuteMultiRequestTestSerie(client, objectServer);
                }
            }
        }

        [TestMethod]
        public void TestWoopsaHugeMultiRequest()
        {
            WoopsaObject serverRoot = new WoopsaObject(null, "");
            TestObjectMultiRequest objectServer = new TestObjectMultiRequest();
            WoopsaObjectAdapter adapter = new WoopsaObjectAdapter(serverRoot, "TestObject", objectServer);
            using (WebServer server = new WebServer(adapter, TestingPort))
            {
                server.Start();
                using (WoopsaClient client = new WoopsaClient(TestingUrl))
                {
                    ExecuteHugeMultiRequestTestSerie(client, objectServer);
                }
            }
        }

        [TestMethod]
        public void TestWoopsaMultiRequestNoRemoteMultiRequestService()
        {
            WoopsaObject serverRoot = new WoopsaObject(null, "");
            TestObjectMultiRequest objectServer = new TestObjectMultiRequest();
            new WoopsaObjectAdapter(serverRoot, "TestObject", objectServer);
            using (WebServer server = new WebServer((IWoopsaContainer)serverRoot, TestingPort))
            {
                server.Start();
                using (WoopsaClient client = new WoopsaClient(TestingUrl))
                {
                    ExecuteMultiRequestTestSerie(client, objectServer);
                }
            }

        }

        #region Private Members

        private void ExecuteMultiRequestTestSerie(WoopsaClient client, TestObjectMultiRequest objectServer)
        {
            WoopsaClientMultiRequest multiRequest = new WoopsaClientMultiRequest();

            WoopsaClientRequest request1 = multiRequest.Write("TestObject/A", 1);
            WoopsaClientRequest request2 = multiRequest.Write("TestObject/B", 2);
            WoopsaClientRequest request3 = multiRequest.Read("TestObject/Sum");
            WoopsaClientRequest request4 = multiRequest.Write("TestObject/C", 4);
            WoopsaClientRequest request5 = multiRequest.Meta("TestObject");

            WoopsaMethodArgumentInfo[] argumentInfosSet = new WoopsaMethodArgumentInfo[]
            {
                new WoopsaMethodArgumentInfo("a", WoopsaValueType.Real),
                new WoopsaMethodArgumentInfo("b", WoopsaValueType.Real)
            };
            WoopsaClientRequest request6 = multiRequest.Invoke("TestObject/Set",
                argumentInfosSet, 4, 5);
            WoopsaClientRequest request7 = multiRequest.Read("TestObject/Sum");
            WoopsaClientRequest request8 = multiRequest.Invoke("TestObject/Click",
                new Dictionary<string, WoopsaValue>());
            WoopsaClientRequest request9 = multiRequest.Read("TestObject/IsClicked");
            WoopsaClientRequest request10 = multiRequest.Write("TestObject/IsClicked", false);
            WoopsaClientRequest request11 = multiRequest.Read("TestObject/IsClicked");

            WoopsaMethodArgumentInfo[] argumentInfosSetS = new WoopsaMethodArgumentInfo[]
            {
                new WoopsaMethodArgumentInfo("value", WoopsaValueType.Text)
            };
            WoopsaClientRequest request12 = multiRequest.Invoke("TestObject/SetS",
                argumentInfosSetS, "Hello");
            WoopsaClientRequest request13 = multiRequest.Read("TestObject/S");
            WoopsaClientRequest request14 = multiRequest.Write("TestObject/S", "ABC");
            WoopsaClientRequest request15 = multiRequest.Read("TestObject/S");

            client.ExecuteMultiRequest(multiRequest);
            Assert.AreEqual(objectServer.A, 4);
            Assert.AreEqual(objectServer.B, 5);
            Assert.AreEqual(request3.Result.ResultType, WoopsaClientRequestResultType.Value);
            Assert.AreEqual(request3.Result.Value, 3.0);
            Assert.AreEqual(request4.Result.ResultType, WoopsaClientRequestResultType.Error);
            Assert.IsNotNull(request4.Result.Error);
            Assert.AreEqual(request5.Result.ResultType, WoopsaClientRequestResultType.Meta);
            Assert.IsNotNull(request5.Result.Meta);
            Assert.AreEqual(request6.Result.ResultType, WoopsaClientRequestResultType.Value);
            Assert.IsNotNull(request6.Result.Value);
            Assert.AreEqual(request6.Result.Value.Type, WoopsaValueType.Null);
            Assert.AreEqual(request7.Result.ResultType, WoopsaClientRequestResultType.Value);
            Assert.IsNotNull(request7.Result.Value);
            Assert.AreEqual(request7.Result.Value, 9.0);
            Assert.AreEqual(request8.Result.ResultType, WoopsaClientRequestResultType.Value);
            Assert.AreEqual(request8.Result.Value.Type, WoopsaValueType.Null);
            Assert.IsTrue(request9.Result.Value);
            Assert.AreEqual(request10.Result.ResultType, WoopsaClientRequestResultType.Value);
            Assert.IsFalse(request11.Result.Value);
            Assert.AreEqual(request12.Result.ResultType, WoopsaClientRequestResultType.Value);
            Assert.AreEqual(request12.Result.Value.Type, WoopsaValueType.Null);
            Assert.AreEqual(request13.Result.ResultType, WoopsaClientRequestResultType.Value);
            Assert.IsNotNull(request13.Result.Value);
            Assert.AreEqual(request13.Result.Value, "Hello");
            Assert.AreEqual(objectServer.S, "ABC");
            Assert.AreEqual(request15.Result.ResultType, WoopsaClientRequestResultType.Value);
            Assert.IsNotNull(request15.Result.Value);
            Assert.AreEqual(request15.Result.Value, "ABC");

        }

        private void ExecuteHugeMultiRequestTestSerie(WoopsaClient client, TestObjectMultiRequest objectServer)
        {
            WoopsaClientMultiRequest multiRequest = new WoopsaClientMultiRequest();
            for (int i = 0; i < 200; i++) // ~200 X 15 requests
            {
                multiRequest.Write("TestObject/A", 1);
                multiRequest.Write("TestObject/B", 2);
                multiRequest.Read("TestObject/Sum");
                multiRequest.Write("TestObject/C", 4);
                multiRequest.Meta("TestObject");

                WoopsaMethodArgumentInfo[] argumentInfosSet = new WoopsaMethodArgumentInfo[]
                {
                new WoopsaMethodArgumentInfo("a", WoopsaValueType.Real),
                new WoopsaMethodArgumentInfo("b", WoopsaValueType.Real)
                };
                multiRequest.Invoke("TestObject/Set",
                    argumentInfosSet, 4, 5);
                multiRequest.Read("TestObject/Sum");
                multiRequest.Invoke("TestObject/Click",
                    new Dictionary<string, WoopsaValue>());
                multiRequest.Read("TestObject/IsClicked");
                multiRequest.Write("TestObject/IsClicked", false);
                multiRequest.Read("TestObject/IsClicked");

                WoopsaMethodArgumentInfo[] argumentInfosSetS = new WoopsaMethodArgumentInfo[]
                {
                new WoopsaMethodArgumentInfo("value", WoopsaValueType.Text)
                };
                multiRequest.Invoke("TestObject/SetS",
                    argumentInfosSetS, "Hello");
                multiRequest.Read("TestObject/S");
                multiRequest.Write("TestObject/S", "ABC");
                multiRequest.Read("TestObject/S");
            }
            client.ExecuteMultiRequest(multiRequest);

        }
        #endregion

        #region Inner classes

        public class TestObjectMultiRequest
        {
            public double A { get; set; }
            public double B { get; set; }
            public string S { get; set; }

            public double Sum => A + B;

            public void Set(double a, double b)
            {
                A = a;
                B = b;
            }

            public void SetS(string value)
            {
                S = value;
            }

            public void Click() { IsClicked = true; }

            public bool IsClicked { get; set; }
        }

        #endregion
    }
}
