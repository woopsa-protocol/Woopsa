using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public class RouteHandlerEmbeddedResources : IHTTPRouteHandler
    {
        private string _baseDirectory;
        private Assembly _assembly;

        public RouteHandlerEmbeddedResources(string baseDirectory, Assembly assembly)
        {
            _baseDirectory = baseDirectory;
            _assembly = assembly;
        }

        public void HandleRequest(HTTPRequest request, HTTPResponse response)
        {
            if (request.Subroute == "")
            {
                //If there really is no file requested, then we send a 404!
                response.WriteError(HTTPStatusCode.NotFound, "Not Found");
                return;
            }

            //Find the requested file
            string[] pathParts = request.Subroute.Split('/');
            StringBuilder builder = new StringBuilder();
            for (var i = 0; i < pathParts.Length - 1; i++)
            {
                builder.Append(pathParts[i].Replace('-', '_')).Append('/');
            }
            builder.Append(pathParts[pathParts.Length - 1]);

            string requestedFile = _baseDirectory + builder.ToString().Replace('/', '.');
           
            //Does this file exist?
            if (HTTPServerUtils.EmbeddedResourceExists(requestedFile, _assembly))
            {
                ServeEmbeddedResource(requestedFile, response);
            }
            else
            {
                //Try to get 'index.html', maybe?
                if ( HTTPServerUtils.EmbeddedResourceExists(requestedFile + "index.html", _assembly) )
                {
                    ServeEmbeddedResource(requestedFile + "index.html", response);
                }
                else
                {
                    response.WriteError(HTTPStatusCode.NotFound, "Not Found");
                }
            }
        }

        public bool AcceptSubroutes
        {
            get { return true; }
        }

        private void ServeEmbeddedResource(string filePath, HTTPResponse response)
        {
            string extension = Path.GetExtension(filePath);
            response.SetHeader(HTTPHeader.ContentType, MIMETypeMap.GetMIMEType(extension));
            response.SetHeader(HTTPHeader.LastModified, File.GetLastWriteTime(_assembly.Location).ToHTTPDate());
            Stream file = _assembly.GetManifestResourceStream(filePath);
            response.WriteStream(file);
            file.Close();
        }
    }
}
