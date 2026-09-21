#region Purpose
// Lets GET-style query contracts build their own route with parameters encoded in the query string, since GETs carry no body.
#endregion

namespace TimeWarp.Foundation.Features;

/// <summary>
/// GET-style request that appends its parameters to the route as a query string.
/// </summary>
public interface IQueryStringRouteProvider:IApiRequest
{
  /// <summary>
  /// Route including the encoded query string for this request.
  /// </summary>
  string GetRouteWithQueryString();
}
