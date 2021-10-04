using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Woopsa
{
    public class EndpointFileSystem : Endpoint
    {
        #region Constructor

        public EndpointFileSystem(string routePrefix, HTTPMethod httpMethod, string baseDirectory)
            :base(routePrefix)
        {
            _baseDirectory = baseDirectory;
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

            //Find the requested file
            string requestedFile = _baseDirectory + (string.IsNullOrEmpty(subRoute) ? string.Empty : Path.DirectorySeparatorChar + WoopsaUtils.RemoveInitialSeparator(subRoute).Replace(WoopsaConst.UrlSeparator, Path.DirectorySeparatorChar));

            if (!IsFileAllowed(requestedFile))
            {
                await context.Response.WriteErrorAsync(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            //Does this file exist?
            if (File.Exists(requestedFile))
            {
                await ServeFile(requestedFile, context.Response);
            }
            else if (Directory.Exists(requestedFile))
            {
                //Try to find the index.html file
                requestedFile += Path.DirectorySeparatorChar + "index.html";
                if (File.Exists(requestedFile))
                {
                    await ServeFile(requestedFile, context.Response);
                }
                else
                {
                    await context.Response.WriteErrorAsync(HttpStatusCode.NotFound, "Not Found");
                }
            }
            else
            {
                await context.Response.WriteErrorAsync(HttpStatusCode.NotFound, "Not Found");
            }
        }

        #endregion

        #region Private Methods

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

        private async Task ServeFile(string filePath, HttpResponse response)
        {
            string extension = Path.GetExtension(filePath);
            response.SetHeader(HTTPHeader.ContentType, MIMETypeMap.GetMIMEType(extension));
            response.SetHeader(HTTPHeader.LastModified, File.GetLastWriteTime(filePath).ToHTTPDate());

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                await stream.CopyToAsync(response.Body);
        }

        #endregion

        #region Fields / Attributes

        private string _baseDirectory;
        private HTTPMethod _httpMethod;

        #endregion
    }
}
