namespace TimeWarp.Architecture.Features;

public interface IQueryStringRouteProvider:IApiRequest
{
  string GetRouteWithQueryString();
}
