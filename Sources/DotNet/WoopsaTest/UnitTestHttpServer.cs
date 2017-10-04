using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using Woopsa;

namespace WoopsaTest
{
    [TestClass]
    public class UnitTestHttpServer
    {
        #region Consts

        public const int TestingPort = 9999;
        public static string TestingUrl => $"http://localhost:{TestingPort}/resources";

        #endregion

        [TestMethod]
        public void TestRouteHandlerEmbeddedResource()
        {
            WoopsaRoot serverRoot = new WoopsaRoot();
            TestObjectServer objectServer = new TestObjectServer();
            WoopsaObjectAdapter adapter = new WoopsaObjectAdapter(serverRoot, "TestObject", objectServer);
            using (WoopsaServer server = new WoopsaServer(serverRoot, TestingPort))
            {
                var test1 = new EmbeddedResource.Class1(); // Force to load assembly
                var test2 = new Woopsa.EmbeddedResource.Class1(); // Force to load assembly
                var routeHandlerEmbeddedResources = new RouteHandlerEmbeddedResources();
                server.WebServer.Routes.Add("resources", HTTPMethod.GET,
                    routeHandlerEmbeddedResources);
                using (HttpClient client = new HttpClient())
                {
                    var request = client.GetAsync($"{TestingUrl}/EmbeddedResource/Images/woopsa-logo.png");
                    request.Wait();
                    Assert.IsTrue(request.Result.IsSuccessStatusCode);
                    request = client.GetAsync($"{TestingUrl}/EmbeddedResource/Images/SubImages/woopsa-logo.png");
                    request.Wait();
                    Assert.IsTrue(request.Result.IsSuccessStatusCode);
                    
                    request = client.GetAsync($"{TestingUrl}/Woopsa.EmbeddedResource/Images/woopsa-logo.png");
                    request.Wait();
                    Assert.IsTrue(request.Result.IsSuccessStatusCode);

                    request = client.GetAsync($"{TestingUrl}/Woopsa.EmbeddedResource/Images/SubImages/woopsa-logo.png");
                    request.Wait();
                    Assert.IsTrue(request.Result.IsSuccessStatusCode);
                }
            }
        }
    }
}
