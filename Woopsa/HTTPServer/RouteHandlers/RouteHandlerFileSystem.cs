using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public class RouteHandlerFileSystem : IHTTPRouteHandler
    {
        private string _baseDirectory;

        public RouteHandlerFileSystem(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
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
            string requestedFile = _baseDirectory + request.Subroute.Replace('/', '\\');

            if (!IsFileAllowed(requestedFile))
            {
                response.WriteError(HTTPStatusCode.NotFound, "Not Found");
                return;
            }
           
            //Does this file exist?
            if (File.Exists(requestedFile))
            {
                ServeFile(requestedFile, response);
            }
            else if (Directory.Exists(requestedFile))
            {
                //Try to find the index.html file
                requestedFile = requestedFile + "index.html";
                if (File.Exists(requestedFile))
                {
                    ServeFile(requestedFile, response);
                }
                else
                {
                    response.WriteError(HTTPStatusCode.NotFound, "Not Found");
                }
            }
            else
            {
                response.WriteError(HTTPStatusCode.NotFound, "Not Found");
            }
        }

        public bool AcceptSubroutes
        {
            get { return true; }
        }

        /*
         * This is a very important function!
         * It tells us if a file/directory requested in
         * 'path' is really inside the _baseDirectory
         */
        private bool IsFileAllowed(string path)
        {
            string realPath = Path.GetFullPath(path);
            string realBase = Path.GetFullPath(_baseDirectory);
            if (realPath.StartsWith(realBase))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void ServeFile(string filePath, HTTPResponse response)
        {
            string extension = Path.GetExtension(filePath);
            response.SetHeader(HTTPHeader.ContentType, MIMETypeMap.GetMIMEType(extension));
            response.SetHeader(HTTPHeader.LastModified, System.IO.File.GetLastWriteTime(filePath).ToHTTPDate());
            FileStream file = File.Open(filePath, FileMode.Open);
            response.WriteStream(file);
            file.Close();
        }
    }
}
