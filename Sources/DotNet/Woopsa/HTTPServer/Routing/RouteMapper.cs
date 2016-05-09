using System.Collections.Generic;

namespace Woopsa
{
    /// <summary>
    /// Provides mechanisms that allow to map a route to a route handler.
    /// This class is used mostly internally, except when adding post-route
    /// processors.
    /// </summary>
    public class RouteMapper
    {
        #region ctor
        internal RouteMapper(string route, HTTPMethod methods, IHTTPRouteHandler handler)
        {
            _route = route;
            _methods = methods;
            _handler = handler;
            _processors = new List<PostRouteProcessor>();
        }
        #endregion

        #region Public Members
        public string Route { get { return _route; } }
        public HTTPMethod Methods { get { return _methods; } }
        #endregion

        #region Private/Protected/Internal Members
        private string _route;
        private HTTPMethod _methods;
        private IHTTPRouteHandler _handler;
        internal bool AcceptSubroutes { get { return _handler.AcceptSubroutes; } }

        private List<PostRouteProcessor> _processors;

        private IEnumerable<IRequestProcessor> _requestProcessors
        {
            get
            {
                foreach (PostRouteProcessor processor in _processors)
                {
                    if (processor is IRequestProcessor)
                    {
                        yield return processor as IRequestProcessor;
                    }
                }
            }
        }

        private IEnumerable<IResponseProcessor> _responseProcessors
        {
            get
            {
                foreach (PostRouteProcessor processor in _processors)
                {
                    if (processor is IResponseProcessor)
                    {
                        yield return processor as IResponseProcessor;
                    }
                }
            }
        }
        #endregion

        #region Public methods
        public void AddProcessor(PostRouteProcessor processor)
        {
            _processors.Add(processor);
        }

        public bool RemoveProcessor(PostRouteProcessor processor)
        {
            return _processors.Remove(processor);
        }
        #endregion

        #region Private/Protected/Internal methods
        internal void HandleRequest(HTTPRequest request, HTTPResponse response)
        {
            foreach (IRequestProcessor processor in _requestProcessors)
            {
                if (!processor.Process(request, response))
                {
                    return;
                }
            }
            _handler.HandleRequest(request, response);
            foreach(IResponseProcessor processor in _responseProcessors)
            {
                processor.Process(request, response);
            }
        }
        #endregion
    }
}
