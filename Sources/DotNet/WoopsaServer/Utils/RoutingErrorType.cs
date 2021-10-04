namespace Woopsa
{
    public enum RoutingErrorType
    {
        MULTIPLE_MATCHES, //If the RouteSolver finds multiple matches for a request, this error is raised for every match (except the first one)
        NO_MATCHES, //The RouteSolver sends a 404 in this case, but the event is raised for debugging purposes
        INTERNAL //All other routing error types
    }
}
