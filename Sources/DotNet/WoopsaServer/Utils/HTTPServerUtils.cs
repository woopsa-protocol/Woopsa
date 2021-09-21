using System;

namespace Woopsa
{
    public static class HTTPServerUtils
    {
        public static string ToHTTPDate(this DateTime now)
        {
            return now.ToUniversalTime().ToString("r");
        }
    }
}