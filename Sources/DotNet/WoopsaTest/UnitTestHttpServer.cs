using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using Woopsa;

namespace WoopsaTest
{
    [TestClass]
    public class UnitTestHttpServer
    {
        #region Consts

        public const int TestingPort = 9999;
        public static string TestingUrl => $"http://localhost:{TestingPort}";

        #endregion

        [TestMethod]
        public void TestRouteHandlerEmbeddedResource()
        {
            WoopsaRoot serverRoot = new WoopsaRoot();
            TestObjectServer objectServer = new TestObjectServer();
            WoopsaObjectAdapter adapter = new WoopsaObjectAdapter(serverRoot, "TestObject", objectServer);
            using WebServer server = new WebServer(serverRoot, port: TestingPort);
            var test1 = new EmbeddedResource.Class1(); // Force to load assembly
            var test2 = new Woopsa.EmbeddedResource.Class1(); // Force to load assembly
            var routeHandlerEmbeddedResources = new EndpointEmbeddedResources("resources", HTTPMethod.GET);
            server.AddEndPoint(routeHandlerEmbeddedResources);
            server.Start();
            using HttpClient client = new HttpClient();
            var request = client.GetAsync($"{TestingUrl}/resources/EmbeddedResource/Images/woopsa-logo.png");
            request.Wait();
            Assert.IsTrue(request.Result.IsSuccessStatusCode);
            request = client.GetAsync($"{TestingUrl}/resources/EmbeddedResource/Images/SubImages/woopsa-logo.png");
            request.Wait();
            Assert.IsTrue(request.Result.IsSuccessStatusCode);
            request = client.GetAsync($"{TestingUrl}/resources/Woopsa.EmbeddedResource/Images/woopsa-logo.png");
            request.Wait();
            Assert.IsTrue(request.Result.IsSuccessStatusCode);
            request = client.GetAsync($"{TestingUrl}/resources/Woopsa.EmbeddedResource/Images/SubImages/woopsa-logo.png");
            request.Wait();
            Assert.IsTrue(request.Result.IsSuccessStatusCode);
        }

        [TestMethod]
        public void TestRouteHandlerMemory()
        {
            WoopsaRoot serverRoot = new WoopsaRoot();
            using WebServer server = new WebServer(serverRoot, port: TestingPort);
            var routeHandlerMemory = new EndpointMemory("resources", HTTPMethod.GET);

            byte[] fileContents = File.ReadAllBytes("../../../TestResources/Hello.txt");
            using MemoryStream memoryStream = new MemoryStream(fileContents);
            routeHandlerMemory.RegisterResource("test", memoryStream);

            server.AddEndPoint(routeHandlerMemory);
            server.Start();

            byte[] fileContents2 = File.ReadAllBytes("../../../TestResources/Glacier.jpg");
            using MemoryStream memoryStream2 = new MemoryStream(fileContents2);
            routeHandlerMemory.RegisterResource("test2", memoryStream);

            using HttpClient client = new HttpClient();

            var request = client.GetAsync($"{TestingUrl}/resources/test");
            request.Wait();
            Assert.IsTrue(request.Result.IsSuccessStatusCode);


            request = client.GetAsync($"{TestingUrl}/resources/test2");
            request.Wait();
            Assert.IsTrue(request.Result.IsSuccessStatusCode);
        }

        [TestMethod]
        public void TestRouteHandlerRedirect()
        {
            string routePrefix = "test";
            string ressourceName = "test2";
            WoopsaRoot serverRoot = new WoopsaRoot();
            using WebServer server = new WebServer(serverRoot, port: TestingPort);
            EndpointMemory routeHandlerMemory = new EndpointMemory("resources", HTTPMethod.GET);
            byte[] fileContents = File.ReadAllBytes("../../../TestResources/Hello.txt");
            using MemoryStream memoryStream = new MemoryStream(fileContents);
            routeHandlerMemory.RegisterResource(ressourceName, memoryStream);
            server.AddEndPoint(routeHandlerMemory);
            EndpointRedirect routeHandlerRedirect = new EndpointRedirect(routePrefix, HTTPMethod.GET, $"{TestingUrl}/resources/{ressourceName}", WoopsaRedirectionType.Permanent);
            server.AddEndPoint(routeHandlerRedirect);
            server.Start();

            using HttpClient client = new HttpClient();
            var request = client.GetAsync($"{TestingUrl}/{routePrefix}");
            request.Wait();
            Assert.IsTrue(request.Result.IsSuccessStatusCode);
        }

        [TestMethod]
        public void TestRouteHandlerFileSystem()
        {
            string routePrefix = "test";
            WoopsaRoot serverRoot = new WoopsaRoot();
            using WebServer server = new WebServer(serverRoot, port: TestingPort);
            EndpointFileSystem routeHandlerFileSystem = new EndpointFileSystem(routePrefix, HTTPMethod.GET | HTTPMethod.POST, @"../../../TestResources");
            server.AddEndPoint(routeHandlerFileSystem);
            server.Start();

            using HttpClient client = new HttpClient();
            var request = client.GetAsync($"{TestingUrl}/{routePrefix}/Hello.txt");

            request.Wait();
            Assert.IsTrue(request.Result.IsSuccessStatusCode);

            request = client.PostAsync($"{TestingUrl}/{routePrefix}/Hello.txt", new StringContent(""));
            request.Wait();
            Assert.IsTrue(request.Result.IsSuccessStatusCode);

            request = client.DeleteAsync($"{TestingUrl}/{routePrefix}/Hello.txt");
            request.Wait();
            Assert.IsFalse(request.Result.IsSuccessStatusCode);
        }
    }
}
