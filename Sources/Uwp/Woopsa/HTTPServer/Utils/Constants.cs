using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    [Flags]
    public enum HTTPMethod
    {
        GET     = 1,
        POST    = 2,
        PUT     = 4,
        DELETE  = 8,
        HEAD    = 16,
        TRACE   = 32,
        OPTIONS = 64,
        PATCH   = 128,
        CONNECT = 256
    }

    public enum HTTPStatusCode
    {
        Continue = 100,
        SwitchingProtocols = 101,
        OK = 200,
        Created = 201,
        Accepted = 202,
        NonAuthoritativeInformation = 203,
        NoContent = 204,
        ResetContent = 205,
        PartialContent = 206,
        MultipleChoices = 300,
        Ambiguous = 300,
        MovedPermanently = 301,
        Moved = 301,
        Found = 302,
        Redirect = 302,
        SeeOther = 303,
        RedirectMethod = 303,
        NotModified = 304,
        UseProxy = 305,
        Unused = 306,
        TemporaryRedirect = 307,
        RedirectKeepVerb = 307,
        BadRequest = 400,
        Unauthorized = 401,
        PaymentRequired = 402,
        Forbidden = 403,
        NotFound = 404,
        MethodNotAllowed = 405,
        NotAcceptable = 406,
        ProxyAuthenticationRequired = 407,
        RequestTimeout = 408,
        Conflict = 409,
        Gone = 410,
        LengthRequired = 411,
        PreconditionFailed = 412,
        RequestEntityTooLarge = 413,
        RequestUriTooLong = 414,
        UnsupportedMediaType = 415,
        RequestedRangeNotSatisfiable = 416,
        ExpectationFailed = 417,
        UpgradeRequired = 426,
        InternalServerError = 500,
        NotImplemented = 501,
        BadGateway = 502,
        ServiceUnavailable = 503,
        GatewayTimeout = 504,
        HttpVersionNotSupported = 505
    }

    public class HTTPHeader
    {
        #region Request Headers
        public const string Host            = "Host";
        public const string Accept          = "Accept";
        public const string AcceptCharset   = "Accept-Charset";
        public const string AcceptEncoding  = "Accept-Encoding";
        public const string AcceptLanguage  = "Accept-Language";
        public const string Cookie          = "Cookie";
        public const string Referer         = "Referer";
        public const string UserAgent       = "User-Agent";
        public const string Authorization   = "Authorization";
        #endregion

        #region Response Headers
        public const string Date            = "Date";
        public const string Server          = "Server";
        public const string LastModified    = "Last-Modified";
        public const string AcceptRanges    = "Accept-Ranges";
        public const string ContentLength   = "Content-Length";
        public const string ContentType     = "Content-Type";
        public const string ContentEncoding = "Content-Encoding";
        public const string TransferEncoding = "Transfer-Encoding";
        public const string WWWAuthenticate = "WWW-Authenticate";
        #endregion

        #region Common Headers
        public const string Connection = "Connection";
        #endregion
    }

    public class MIMETypes
    {
        public class Text
        {
            public const string Plain = "text/plain";
            public const string HTML = "text/html";
        }

        public class Application
        {
            public const string JSON = "application/json";
            public const string OctetStream = "application/octet-stream";
            public const string XWWWFormUrlEncoded = "application/x-www-form-urlencoded";
        }

        public class Image
        {
            public const string JPEG = "image/jpeg";
            public const string PNG = "image/png";
            public const string GIF = "image/gif";
        }
    }
}
