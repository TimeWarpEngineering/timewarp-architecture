namespace TimeWarp.Foundation.Features;

public interface IQueryStringRouteProvider:IApiRequest
{
  string GetRouteWithQueryString();
}
