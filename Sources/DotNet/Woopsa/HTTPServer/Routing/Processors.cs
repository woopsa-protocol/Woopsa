using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    /// <summary>
    /// A Post-Route Processor is a type of processing layer which
    /// happens just before the RouteHandler can get to it. This means
    /// the Route Solver has already determined which route the URL
    /// corresponds to.
    /// </summary>
    public abstract class PostRouteProcessor { }

    /// <summary>
    /// A Pre-Route Processor is a type of processing layer which
    /// happens before any route processing. This means that the
    /// request can still go to any other Route Handler, or that
    /// the HTTP request will lead to a 404, for example. It's very
    /// useful for making a TLS or HTTP 2 layer, for example.
    /// </summary>
    public abstract class PreRouteProcessor 
    {
        public abstract Stream StartProcessStream(Stream input);
        public abstract void EndProcessStream(Stream input);
    }

    /// <summary>
    /// A Request Processor is a processor which needs to process
    /// the data <b>before</b> a RouteHandler has generated a response
    /// to a request.
    /// <para>
    /// For example, a post-route request processor could be used
    /// to add authentication, while a pre-route response processor could be used
    /// to decrypt the TLS layer or act as a very simple proxy.
    /// </para>
    /// </summary>
    public interface IRequestProcessor
    {
        /// <summary>
        /// The function which is called automatically by the web server.
        /// </summary>
        /// <param name="request">The request</param>
        /// <param name="response">The response</param>
        /// <returns>
        /// If this function returns <b>true</b>, the standard routing
        /// mechanism continues after this call, meaning the route processing
        /// is transparent. If this function returns <b>false</b>, then 
        /// processing is stopped short.
        /// </returns>
        bool Process(HTTPRequest request, HTTPResponse response);
    }

    /// <summary>
    /// A Response Processor is a processor which needs to process
    /// the data <b>after</b> a RouteHandler has generated a response
    /// to a request.
    /// <para>
    /// For example, a post-route response processor could be used
    /// to add cookies, while a pre-route response processor could be used
    /// to compress the response in gzip/deflate or encrypt the result
    /// using the TLS layer.
    /// </para>
    /// </summary>
    public interface IResponseProcessor
    {
        /// <summary>
        /// The function which is called automatically by the web server.
        /// Because this is a response processor, the user can call
        /// <see cref="WebServer.HTTPResponse.Respond"/> on the response
        /// knowing that the response has already been prepared.
        /// </summary>
        /// <param name="request">The request</param>
        /// <param name="response">The response</param>
        void Process(HTTPRequest request, HTTPResponse response);
    }
}
