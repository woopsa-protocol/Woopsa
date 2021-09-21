using Ionic.Zip;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Woopsa
{
    /// <summary>
    /// Note : To use RouteHandlerEmbeddedResources the assembly default namespace must correspond to the assembly name.
    /// </summary>
    public class EndpointEmbeddedResources : Endpoint
    {
        #region Constructor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resourcePathInAssembly">
        /// The path where to find resources within the assembly. Empty string "" means the whole assembly.
        /// HTTP route should contain the remaining path from this origin path to the resource.
        /// </param>
        /// <param name="assembly">
        /// The assembly in which the resources are searched for.
        /// When assembly is null, the first part of the http route path is used as the assembly name. 
        /// An assembly with that name is searched in all loaded assemblies.
        /// </param>
        public EndpointEmbeddedResources(string routePrefix, HTTPMethod httpMethod, string resourcePathInAssembly = "", Assembly assembly = null)
            : base(routePrefix)
        {
            if (resourcePathInAssembly != string.Empty)
                _baseDirectory = ResourcePath(resourcePathInAssembly);
            else
                _baseDirectory = "";
            _assembly = assembly;
            _httpMethod = httpMethod;
        }
        
        #endregion

        #region IHTTPRouteHandler

        public override async Task HandleRequestAsync(HttpContext context, string subRoute)
        {
            if ((_httpMethod & (HTTPMethod)Enum.Parse(typeof(HTTPMethod), context.Request.Method)) == 0)
            {
                await context.Response.WriteErrorAsync(HttpStatusCode.NotFound, "Not Found");
                return;
            }
            if (string.IsNullOrEmpty(subRoute))
            {
                await context.Response.WriteErrorAsync(HttpStatusCode.NotFound, "Not Found");
                return;
            }
            if (_assembly != null)
                await RespondResourceAsync(context.Response, _assembly, subRoute);
            else
            {
                Assembly assembly;
                // First element = initial /
                subRoute = WoopsaUtils.RemoveInitialAndFinalSeparator(subRoute);
                string[] pathParts = subRoute.Split(new char[] { WoopsaConst.UrlSeparator }, 2);
                if (pathParts.Length == 2 && pathParts[0] != string.Empty)
                    assembly = AssemblyByName(pathParts[0]);
                else
                    assembly = null;
                if (assembly != null)
                    await RespondResourceAsync(context.Response, assembly, WoopsaConst.UrlSeparator + pathParts[1]);
                else
                    await context.Response.WriteErrorAsync(HttpStatusCode.NotFound, string.Format("Assembly not Found at '{0}'", subRoute));
            }
        }

        #endregion IHTTPRouteHandler

        #region Constants

        const char ResourcePathSeparator = '.';

        #endregion

        #region Private Methods

        private async Task RespondResourceAsync(HttpResponse response, Assembly assembly, string subRoute)
        {
            if (subRoute != string.Empty)
            {
                string requestedFile = ResourcePath(subRoute);
                if (_baseDirectory != string.Empty)
                    if (requestedFile != string.Empty)
                        requestedFile = _baseDirectory + ResourcePathSeparator + requestedFile;
                    else
                        requestedFile = _baseDirectory;
                // Does this resource exist?
                if (!await ServeEmbeddedResourceAsync(response, assembly, requestedFile))
                {
                    string indexHtmlFileResource = requestedFile + ResourcePathSeparator + "index.html";
                    //Try to get 'index.html', maybe?
                    if (!await ServeEmbeddedResourceAsync(response, assembly, indexHtmlFileResource))
                        await response.WriteErrorAsync(HttpStatusCode.NotFound, string.Format("Resource not Found at '{0}'", subRoute));
                }
            }
            else
                //If there really is no file requested, then we send a 404!
                await response.WriteErrorAsync(HttpStatusCode.NotFound, string.Format("Resource not Found at '{0}'", subRoute));
        }

        private string ResourcePath(string urlPath)
        {
            return urlPath.
                Trim(WoopsaConst.UrlSeparator).
                Replace(WoopsaConst.UrlSeparator, ResourcePathSeparator);
        }

        private async Task<bool> ServeEmbeddedResourceAsync(HttpResponse response, Assembly assembly,
            string strippedResourceName)
        {
            string fullResourceName = FullResourceName(assembly, strippedResourceName);
            if (!string.IsNullOrEmpty(fullResourceName))
                try
                {
                    using Stream resourceStream = assembly.GetManifestResourceStream(fullResourceName);
                    if (resourceStream != null)
                    {
                        string extension = Path.GetExtension(strippedResourceName);
                        response.Headers[HTTPHeader.ContentType] = MIMETypeMap.GetMIMEType(extension);
                        response.Headers[HTTPHeader.LastModified] = File.GetLastWriteTime(assembly.Location).ToHTTPDate();
                        await resourceStream.CopyToAsync(response.Body);
                        return true;
                    }
                }
                catch
                {
                }
            return false;
        }

        private string FullResourceName(Assembly assembly, string strippedResourceName)
        {
            string safeDefaultNamesapceFromAssemblyName = assembly.GetName().Name.Replace(" ", "_");
            return string.Format("{0}.{1}", safeDefaultNamesapceFromAssemblyName, strippedResourceName);
        }

        private Assembly AssemblyByName(string name)
        {
            Assembly result;

            if (_assemblyByName == null)
            {
                _assemblyByName = new Dictionary<string, Assembly>();
                result = null;
            }
            else
                _assemblyByName.TryGetValue(name, out result);

            if (result == null)
            {
                result = AppDomain.CurrentDomain.GetAssemblies().
                      SingleOrDefault(assembly => assembly.GetName().Name == name);
                if (result != null)
                    _assemblyByName[result.GetName().Name] = result;
            }
            return result;
        }

        #endregion

        #region Fields / Attributes

        private string _baseDirectory;
        private Assembly _assembly;
        private Dictionary<string, Assembly> _assemblyByName;
        private HTTPMethod _httpMethod;

        #endregion
    }

    /// <summary>
    /// Note : To use RouteHandlerEmbeddedResources the assembly default namespace must correspond to the assembly name.
    /// </summary>
    public class EndpointZipEmbeddedResources : Endpoint
    {
        #region Constructor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resourcePathInAssembly">
        /// The path where to find resources within the assembly. Empty string "" means the whole assembly.
        /// HTTP route should contain the remaining path from this origin path to the resource.
        /// </param>
        /// <param name="assembly">
        /// The assembly in which the resources are searched for.
        /// When assembly is null, the first part of the http route path is used as the assembly name. 
        /// An assembly with that name is searched in all loaded assemblies.
        /// </param>
        public EndpointZipEmbeddedResources(string routePrefix, HTTPMethod httpMethod, string assemblyName, string zipPathInAssembly)
            : base(routePrefix)
        {
            _zipPathInAssembly = zipPathInAssembly;
            _httpMethod = httpMethod;
            _assemblyName = assemblyName;
            _assembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == _assemblyName);
        }

        #endregion

        #region IHTTPRouteHandler

        public override async Task HandleRequestAsync(HttpContext context, string subRoute)
        {
            if ((_httpMethod & (HTTPMethod)Enum.Parse(typeof(HTTPMethod), context.Request.Method)) == 0)
            {
                await context.Response.WriteErrorAsync(HttpStatusCode.NotFound, "Not Found");
                return;
            }
            else
                await RespondResourceAsync(context.Response, _assembly, subRoute);
        }

        #endregion IHTTPRouteHandler

        #region Constants

        const char ResourcePathSeparator = '.';

        #endregion

        #region Private Methods

        private async Task RespondResourceAsync(HttpResponse response, Assembly assembly, string subRoute)
        {
            if (subRoute != string.Empty)
            {
                subRoute ??= string.Empty;
                string requestedFile = ResourcePath(_zipPathInAssembly);
                if (!string.IsNullOrEmpty(subRoute))
                {
                    // Does this resource exist?
                    if (!await ServeEmbeddedResourceAsync(response, assembly, requestedFile, subRoute))
                    {
                        string indexHtmlFileResource = subRoute + WoopsaConst.UrlSeparator + "index.html";
                        //Try to get 'index.html', maybe?
                        if (!await ServeEmbeddedResourceAsync(response, assembly, requestedFile, indexHtmlFileResource))
                            await response.WriteErrorAsync(HttpStatusCode.NotFound, string.Format("Resource not Found at '{0}'", subRoute));
                    }
                }
                // Does this resource exist?
                else
                {
                    string indexHtmlFileResource = subRoute + WoopsaConst.UrlSeparator + "index.html";
                    //Try to get 'index.html', maybe?
                    if (!await ServeEmbeddedResourceAsync(response, assembly, requestedFile, indexHtmlFileResource))
                        await response.WriteErrorAsync(HttpStatusCode.NotFound, string.Format("Resource not Found at '{0}'", subRoute));
                }
            }
            else
                //If there really is no file requested, then we send a 404!
                await response.WriteErrorAsync(HttpStatusCode.NotFound, string.Format("Resource not Found at '{0}'", subRoute));
        }

        private string ResourcePath(string urlPath)
        {
            return urlPath.
                Trim(WoopsaConst.UrlSeparator).
                Replace(WoopsaConst.UrlSeparator, ResourcePathSeparator);
        }

        private async Task<bool> ServeEmbeddedResourceAsync(HttpResponse response, Assembly assembly,
            string strippedResourceName, string subRoute)
        {
            string fullResourceName = FullResourceName(assembly, strippedResourceName);
            if (!string.IsNullOrEmpty(fullResourceName))
                try
                {
                    using Stream resourceStream = assembly.GetManifestResourceStream(fullResourceName);
                    resourceStream.Seek(0, SeekOrigin.Begin);
                    using (ZipFile zip = ZipFile.Read(resourceStream))
                    {                      
                        ZipEntry zipEntry = zip[WoopsaConst.UrlSeparator + WoopsaUtils.RemoveInitialSeparator(subRoute)];
                        Stream resourceStream2 = zipEntry.OpenReader();
                        if (resourceStream2 != null)
                        {
                            string extension = Path.GetExtension(subRoute);
                            response.Headers[HTTPHeader.ContentType] = MIMETypeMap.GetMIMEType(extension);
                            response.Headers[HTTPHeader.LastModified] = File.GetLastWriteTime(assembly.Location).ToHTTPDate();
                            await resourceStream2.CopyToAsync(response.Body);
                            return true;
                        }
                    }
                }
                catch(Exception ex)
                {
                    ;
                }
            return false;
        }

        private string FullResourceName(Assembly assembly, string strippedResourceName)
        {
            string safeDefaultNamesapceFromAssemblyName = assembly.GetName().Name.Replace(" ", "_");
            return string.Format("{0}.{1}", safeDefaultNamesapceFromAssemblyName, strippedResourceName);
        }

        #endregion

        #region Fields / Attributes

        private string _assemblyName;
        private string _zipPathInAssembly;
        private Assembly _assembly;
        private HTTPMethod _httpMethod;

        #endregion
    }
}
