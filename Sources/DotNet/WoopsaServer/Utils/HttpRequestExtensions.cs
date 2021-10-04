using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public static class HttpRequestExtensions
    {
        public static async Task WriteErrorAsync(this HttpResponse httpResponse, HttpStatusCode errorCode, string errorMessage)
        {
            errorMessage = RemoveNotAllowedChar(errorMessage);
            httpResponse.StatusCode = (int)errorCode;
            httpResponse.Headers[HTTPHeader.Connection] = HTTPHeader.CloseConnectionResponseHeaderValue;
            await httpResponse.WriteAsync(errorMessage);
        }

        public static string SubRoute(this HttpRequest httpRequest) 
        {
            string path = httpRequest.Path;
            return path.Split(WoopsaConst.UrlSeparator, 3)[2];
        }

        public static void SetHeader(this HttpResponse httpResponse, string header, string value)
        {
            CheckForNotSupportedchar(value);
            httpResponse.Headers[header] = value;
        }

        private static void CheckForNotSupportedchar(string value)
        {
            if (value.Contains("\r\n") || value.Contains('\n') || value.Contains('\r'))
                throw new Exception($"Value {value} contains not supported char such as \\n\\r, \\n or \\r");
        }

        private static string RemoveNotAllowedChar(string text) =>
            text.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
    }
}
