using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Abstractions;

namespace Woopsa
{
    public static class WoopsaAspExtensions
    {
        static public IEndpointConventionBuilder MapWoopsa(this IEndpointRouteBuilder endpointRouteBuilder, Endpoint endpoint)
        {
            return endpoint.MapEndPoint(endpointRouteBuilder);
        }
    }
}
