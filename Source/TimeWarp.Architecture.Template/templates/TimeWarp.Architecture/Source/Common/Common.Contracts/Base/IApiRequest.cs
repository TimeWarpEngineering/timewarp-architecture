namespace TimeWarp.Architecture.Features;

public interface IApiRequest
{
  string GetRoute();
  HttpVerb GetHttpVerb();
}
