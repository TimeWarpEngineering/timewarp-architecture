#region Purpose
// Lets GET-style query contracts build their own route with parameters encoded in the query string, since GETs carry no body.
#endregion

namespace TimeWarp.Foundation.Features;

public interface IQueryStringRouteProvider:IApiRequest
{
  string GetRouteWithQueryString();
}
