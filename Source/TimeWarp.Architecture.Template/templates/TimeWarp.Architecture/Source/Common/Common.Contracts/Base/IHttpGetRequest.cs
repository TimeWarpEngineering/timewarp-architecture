namespace TimeWarp.Architecture.Features;

public interface IHttpGetRequest:IApiRequest
{
  string GetRouteWithQueryString();
}
