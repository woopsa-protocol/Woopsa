using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Woopsa;
using System.Collections.Generic;

namespace WoopsaTest
{
    public class TestObjectMultiRequest
    {
        public double A { get; set; }
        public double B { get; set; }
        public string S { get; set; }

        public double Sum { get { return A + B; } }

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

    [TestClass]
    public class UnitTestWoopsaMultiRequest
    {
        [TestMethod]
        public void TestWoopsaMultiRequest()
        {
            WoopsaObject serverRoot = new WoopsaObject(null, "");
            TestObjectMultiRequest objectServer = new TestObjectMultiRequest();
            WoopsaObjectAdapter adapter = new WoopsaObjectAdapter(serverRoot, "TestObject", objectServer);
            using (WoopsaServer server = new WoopsaServer(serverRoot))
            {
                using (WoopsaClient client = new WoopsaClient("http://localhost/woopsa"))
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
                        new Dictionary<string,string>());
                    WoopsaMethodArgumentInfo[] argumentInfosSetS = new WoopsaMethodArgumentInfo[]
                    {
                        new WoopsaMethodArgumentInfo("value", WoopsaValueType.Text)
                    };
                    WoopsaClientRequest request9 = multiRequest.Invoke("TestObject/SetS",
                        argumentInfosSetS, "Hello");
                    WoopsaClientRequest request10 = multiRequest.Read("TestObject/S");
                    WoopsaClientRequest request11 = multiRequest.Write("TestObject/S", "ABC");
                    WoopsaClientRequest request12 = multiRequest.Read("TestObject/S");

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
                    Assert.AreEqual(request9.Result.ResultType, WoopsaClientRequestResultType.Value);
                    Assert.AreEqual(request9.Result.Value.Type, WoopsaValueType.Null);
                    Assert.IsTrue(objectServer.IsClicked);
                    Assert.AreEqual(request10.Result.ResultType, WoopsaClientRequestResultType.Value);
                    Assert.IsNotNull(request10.Result.Value);
                    Assert.AreEqual(request10.Result.Value, "Hello");
                    Assert.AreEqual(objectServer.S, "ABC");
                    Assert.AreEqual(request12.Result.ResultType, WoopsaClientRequestResultType.Value);
                    Assert.IsNotNull(request12.Result.Value);
                    Assert.AreEqual(request12.Result.Value, "ABC");
                }
            }
        }

        [TestMethod]
        public void TestWoopsaMultiRequestNoRemoteMultiRequestService()
        {
            WoopsaObject serverRoot = new WoopsaObject(null, "");
            TestObjectMultiRequest objectServer = new TestObjectMultiRequest();
            WoopsaObjectAdapter adapter = new WoopsaObjectAdapter(serverRoot, "TestObject", objectServer);
            using (WoopsaServer server = new WoopsaServer((IWoopsaContainer)serverRoot))
            {
                using (WoopsaClient client = new WoopsaClient("http://localhost/woopsa"))
                {
                    WoopsaClientMultiRequest multiRequest = new WoopsaClientMultiRequest();

                    WoopsaClientRequest request1 = multiRequest.Write("TestObject/A", 1);
                    WoopsaClientRequest request2 = multiRequest.Write("TestObject/B", 2);
                    WoopsaClientRequest request3 = multiRequest.Read("TestObject/Sum");
                    WoopsaClientRequest request4 = multiRequest.Write("TestObject/C", 4);
                    WoopsaClientRequest request5 = multiRequest.Meta("TestObject");

                    WoopsaMethodArgumentInfo[] argumentInfos = new WoopsaMethodArgumentInfo[]
                    {
                        new WoopsaMethodArgumentInfo("a", WoopsaValueType.Real),
                        new WoopsaMethodArgumentInfo("b", WoopsaValueType.Real)
                    };
                    WoopsaClientRequest request6 = multiRequest.Invoke("TestObject/Set",
                        argumentInfos, 4, 5);
                    WoopsaClientRequest request7 = multiRequest.Read("TestObject/Sum");

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
                }
            }

        }
    }
}
