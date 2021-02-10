using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Woopsa
{
    /// <summary>
    /// Provides a mechanism to send a response to a request. 
    /// <para>
    /// This class is created internally by the Web Server and thus cannot be created from
    /// outside code. To handle requestsList and response, the <see cref="WebServer.IHTTPRouteHandler"/> 
    /// interface or the delegate methods in <see cref="RouteSolver"/> which is available from the
    /// <see cref="WebServer.WebServer.Routes"/> property.
    /// </para>
    /// <para>
    /// When using a method such as 
    /// WriteString, WriteHTML, SetHeader, etc., no data is actually sent to the client.
    /// Instead, data is buffered until the RouteSolver is done processing, which
    /// will automatically send the response to the client.
    /// </para>
    /// </summary>
    public class HTTPResponse : IDisposable
    {
        #region ctor
        internal HTTPResponse()
        {
            _headers = new Dictionary<string, string>();
            _bufferStream = new MemoryStream();
            _bufferWriter = new StreamWriter(_bufferStream);
            ResponseCode = 200;
            ResponseMessage = "OK";
        }
        #endregion

        #region Public Members
        internal IEnumerable<string> Headers
        {
            get
            {
                foreach (string header in _headers.Keys)
                {
                    yield return header + ": " + _headers[header];
                }
            }
        }

        /// <summary>
        /// Mainly used for debugging, this property returns the Response Code,
        /// commonly known in the standard as a HTTP Status Code,
        /// that will be sent to the client. For a description of HTTP Response
        /// Codes, see <see cref="WebServer.HTTPStatusCode"/>
        /// </summary>
        public int ResponseCode { get; private set; }

        /// <summary>
        /// Mainly used for debugging, this property returns the Response Message
        /// that will be sent to the client. This is the text that immediately follows
        /// the Status Code in an HTTP response, such as "200 OK" or "404 Not Found"
        /// </summary>
        public string ResponseMessage 
        { 
            get => _responseMessage;
            private set 
            { 
                CheckForNotSupportedchar(value);
                _responseMessage = value;
            }
        }
        private string _responseMessage;
        /// <summary>
        /// Mainly used for debugging, this property returns the length, in bytes,
        /// of the response content that will be sent to the client (excluding headers).
        /// </summary>
        public long ResponseLength { get; private set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the value of an HTTP Response Header. Many standard headers are defined in
        /// <see cref="WebServer.HTTPHeader"/>, but custom headers can always be manually
        /// set in case this is desired.
        /// <para>
        /// Headers are sent in the <c>Header: Value</c> format in the HTTP protocol.
        /// </para>
        /// </summary>
        /// <param name="header">
        /// The name of the header, for example:
        /// <list type="bullet">
        /// <item><see cref="WebServer.HTTPHeader.ContentType"/></item>
        /// <item>"My-Custom-Header"</item>
        /// </list>
        /// </param>
        /// <param name="value">
        /// The value of the header.
        /// </param>
        public void SetHeader(string header, string value)
        {
            CheckForNotSupportedchar(value);
            _headers[header] = value;
        }

        /// <summary>
        /// Set the status and message code of the response
        /// </summary>
        /// <param name="responseCode"></param>
        /// <param name="responseMessage"></param>
        public void SetStatusCode(int responseCode, string responseMessage)
        {
            ResponseCode = responseCode;
            ResponseMessage = responseMessage;
        }

        /// <summary>
        /// Writes a specified string to the response buffer. Does not modify the
        /// Content-Type header. Default Content-Type is <c>text/plain</c>
        /// </summary>
        /// <param name="text">
        /// The text to write. Encoding will be solved automatically.
        /// </param>
        public void WriteString(string text)
        {
            _bufferWriter.Write(text);
        }

        /// <summary>
        /// Writes a specified string to the response buffer and sets the Content-Type
        /// header to be <c>text/html</c>, allowing browsers to render the string as HTML.
        /// </summary>
        /// <param name="html">
        /// The text to write. Encoding will be solved automatically.
        /// </param>
        public void WriteHTML(string html)
        {
            SetHeader(HTTPHeader.ContentType, MIMETypes.Text.HTML);
            WriteString(html);
        }

        /// <summary>
        /// Writes a stream to the response buffer. This method is used mainly when serving
        /// files in a more efficient way. This method does not modify the Content-Type header
        /// and if nothing is done, the default value of <c>text/plain</c> will thus be used.
        /// </summary>
        /// <param name="stream"></param>
        public void WriteStream(Stream stream)
        {
            stream.CopyTo(_bufferStream);
        }

        /// <summary>
        /// Sends a nicely-formatted error with specified HTTP Error Code and Error Message
        /// to the client. This response is not buffered and thus calling this method will 
        /// immediately send the response to the client.
        /// </summary>
        /// <param name="errorCode">The HTTP Status Code to send</param>
        /// <param name="errorMessage">The Error Message to send. For example "Not Found" or "Not Supported"</param>
        public void WriteError(HTTPStatusCode errorCode, string errorMessage)
        {
            SendError((int)errorCode, errorMessage);
        }

        /// <summary>
        /// Sends a custom-formatted error with specified HTTP Error Code, Error Message
        /// Response Content and mime type
        /// to the client. This response is not buffered and thus calling this method will 
        /// immediately send the response to the client.
        /// </summary>
        /// <param name="errorCode">The HTTP Status Code to send</param>
        /// <param name="errorMessage">The Error Message to send. For example "Not Found" or "Not Supported"</param>
        public void WriteError(HTTPStatusCode errorCode, string errorMessage, string errorContent, string mimeType)
        {
            SendError((int)errorCode, errorMessage, errorContent, mimeType);
        }

        /// <summary>
        /// Writes the response to the output stream.
        /// <para>
        /// This method should only be called in certain special cases
        /// where the route solving system is being short-circuited somehow,
        /// because the route solving system is usually in charge of calling
        /// this method.
        /// </para>
        /// </summary>
        /// <param name="outputStream">The stream in which to copy the output. Does not close the stream</param>
        public void Respond(Stream outputStream)
        {
            try
            {
                MemoryStream responseStream = new MemoryStream();
                _bufferWriter.Flush();
                SetHeaderIfNotExists(HTTPHeader.ContentType, MIMETypes.Text.Plain);
                ResponseLength = _bufferStream.Length;
                SetHeader(HTTPHeader.ContentLength, ResponseLength.ToString());
                SetHeader(HTTPHeader.Date, DateTime.Now.ToHTTPDate());
                using (StreamWriter writer = new StreamWriter(responseStream, new UTF8Encoding(false), 4096)) 
                {
                    writer.Write("HTTP/1.1 " + ResponseCode + " " + ResponseMessage + "\r\n");
                    foreach (string header in Headers)
                        writer.Write(header + "\r\n");
                    writer.Write("\r\n");
                    writer.Flush();

                    _bufferStream.Position = 0;
                    _bufferStream.CopyTo(responseStream);
                    responseStream.Position = 0;
                    responseStream.CopyTo(outputStream);
                }
            }
            catch (IOException e)
            {
                // This error will be "caught" by the Web Server and evented up so the
                // user can do whatever he wants (or not) with it
                DoError(this, e);
            }
        }
        #endregion

        #region Private Members        
        private Stream _bufferStream;
        private StreamWriter _bufferWriter;
        private Dictionary<string, string> _headers;
        #endregion

        #region Private/Protected/Internal Methods
        private void CheckForNotSupportedchar(string value)
        {
            if (value.Contains("\r\n") || value.Contains('\n') || value.Contains('\r'))
                throw new Exception($"Value {value} contains not supported char such as \\n\\r, \\n or \\r");
        }

        private void SetHeaderIfNotExists(string header, string value)
        {
            if (!_headers.ContainsKey(header))
            {
                _headers[header] = value;
            }
        }

        private void SendError(int errorCode, string errorMessage)
        {
            ResponseCode = errorCode;
            ResponseMessage = errorMessage;
            string errorPage = HTTPServerUtils.GetEmbeddedResource("Woopsa.HTTPServer.HTML.ErrorPage.html");
            errorPage = errorPage.Replace("@ErrorCode", errorCode.ToString());
            errorPage = errorPage.Replace("@ErrorMessage", errorMessage);
            errorPage = errorPage.Replace("@Version", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            WriteString(errorPage);
            SetHeader(HTTPHeader.ContentType, MIMETypes.Text.HTML);
            SetHeader(HTTPHeader.Connection, "close");
        }

        private void SendError(int errorCode, string errorMessage, string responseContent, string mimeType)
        {
            ResponseCode = errorCode;
            ResponseMessage = errorMessage;
            WriteString(responseContent);
            SetHeader(HTTPHeader.ContentType, mimeType);
            SetHeader(HTTPHeader.Connection, "close");
        }
        #endregion

        internal static event EventHandler<HTTPResponseErrorEventArgs> Error;

        protected static void DoError(HTTPResponse response, Exception exception)
        {
            if (Error != null)
            {
                Error(response, new HTTPResponseErrorEventArgs(exception));
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _bufferWriter.Dispose();
                _bufferStream.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public class HTTPResponseErrorEventArgs : EventArgs
    {
        public HTTPResponseErrorEventArgs(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; set; }
    }
}
