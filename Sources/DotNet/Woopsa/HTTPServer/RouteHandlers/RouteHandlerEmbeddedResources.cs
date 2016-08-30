using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Woopsa
{
    public class RouteHandlerEmbeddedResources : IHTTPRouteHandler
    {
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
        public RouteHandlerEmbeddedResources(string resourcePathInAssembly = "", Assembly assembly = null)
        {
            if (resourcePathInAssembly != string.Empty)
                _baseDirectory = ResourcePath(resourcePathInAssembly);
            else
                _baseDirectory = "";
            _assembly = assembly;
            _resourceNameByStrippedResourceNameByAssembly =
                new Dictionary<Assembly, Dictionary<string, string>>();
        }

        #region IHTTPRouteHandler

        public void HandleRequest(HTTPRequest request, HTTPResponse response)
        {
            string subRoute = request.Subroute;
            if (_assembly != null)
                RespondResource(response, _assembly, subRoute);
            else
            {
                Assembly assembly;
                // First element = initial /
                string[] pathParts = subRoute.Split(new char[] { WoopsaConst.UrlSeparator }, 3);
                if (pathParts.Length == 3 && pathParts[1] != "")
                    assembly = AssemblyByName(pathParts[1]);
                else
                    assembly = null;
                if (assembly != null)
                    RespondResource(response, assembly, WoopsaConst.UrlSeparator + pathParts[2]);
                else
                    response.WriteError(HTTPStatusCode.NotFound,
                        string.Format("Assembly not Found at '{0}'", subRoute));
            }
        }

        public bool AcceptSubroutes
        {
            get { return true; }
        }

        #endregion IHTTPRouteHandler

        const char ResourcePathSeparator = '.';

        private void RespondResource(HTTPResponse response, Assembly assembly, string subRoute)
        {
            if (subRoute != "")
            {
                string requestedFile = ResourcePath(subRoute);
                if (_baseDirectory != "")
                    if (requestedFile != "")
                        requestedFile = _baseDirectory + ResourcePathSeparator + requestedFile;
                    else
                        requestedFile = _baseDirectory;
                // Does this resource exist?
                if (!ServeEmbeddedResource(response, assembly, requestedFile))
                {
                    string indexHtmlFileResource = requestedFile + ResourcePathSeparator + "index.html";
                    //Try to get 'index.html', maybe?
                    if (!ServeEmbeddedResource(response, assembly, indexHtmlFileResource))
                        response.WriteError(HTTPStatusCode.NotFound,
                            string.Format("Resource not Found at '{0}'", subRoute));
                }
            }
            else
                //If there really is no file requested, then we send a 404!
                response.WriteError(HTTPStatusCode.NotFound,
                    string.Format("Resource not Found at '{0}'", subRoute));
        }

        private string ResourcePath(string urlPath)
        {
            return urlPath.
                Trim(WoopsaConst.UrlSeparator).
                Replace(WoopsaConst.UrlSeparator, ResourcePathSeparator);
        }

        private bool ServeEmbeddedResource(HTTPResponse response, Assembly assembly,
            string strippedResourceName)
        {
            string fullResourceName = FullResourceName(assembly, strippedResourceName);
            if (!string.IsNullOrEmpty(fullResourceName))
                try
                {
                    using (Stream resourceStream = assembly.GetManifestResourceStream(fullResourceName))
                        if (resourceStream != null)
                        {
                            string extension = Path.GetExtension(strippedResourceName);
                            response.SetHeader(HTTPHeader.ContentType, MIMETypeMap.GetMIMEType(extension));
                            response.SetHeader(HTTPHeader.LastModified, File.GetLastWriteTime(assembly.Location).ToHTTPDate());
                            response.WriteStream(resourceStream);
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
            Dictionary<string, string> resourceNameByStrippedResourceName;
            string result;
            if (!_resourceNameByStrippedResourceNameByAssembly.TryGetValue(
                    assembly, out resourceNameByStrippedResourceName))
            {
                resourceNameByStrippedResourceName = new Dictionary<string, string>();
                _resourceNameByStrippedResourceNameByAssembly[assembly] =
                    resourceNameByStrippedResourceName;
            }
            if (!resourceNameByStrippedResourceName.TryGetValue(strippedResourceName, out result))
            {
                if (resourceNameByStrippedResourceName.Count == 0)
                    // Populate the dictionary
                    foreach (var item in assembly.GetManifestResourceNames())
                    {
                        string[] resourceNameParts = item.Split(new char[] { ResourcePathSeparator }, 2);
                        if (resourceNameParts.Length == 2)
                        {
                            resourceNameByStrippedResourceName[resourceNameParts[1]] = item;
                            if (resourceNameParts[1] == strippedResourceName)
                                result = item;
                        }
                    }
            }
            return result;
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

        private string _baseDirectory;
        private Assembly _assembly;
        private Dictionary<string, Assembly> _assemblyByName;
        private Dictionary<Assembly, Dictionary<string, string>>
            _resourceNameByStrippedResourceNameByAssembly;
    }

}
