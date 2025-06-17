namespace TimeWarp.Architecture.Features;

public interface IApiRequest : IBaseRequest
{
  string GetRoute();
  HttpVerb GetHttpVerb();
}
