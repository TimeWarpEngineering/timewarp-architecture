namespace TimeWarp.Foundation.Features;

public interface IApiRequest : IBaseRequest
{
  string GetRoute();
  HttpVerb GetHttpVerb();
}
