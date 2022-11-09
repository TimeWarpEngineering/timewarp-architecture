namespace TimeWarp.Architecture.Features.Hello;

using TimeWarp.Architecture.Features;

public record HelloRequest : BaseRequest, IApiRequest, IRequest<HelloResponse>
{
  public const string Route = "Hello";

  public HttpVerb GetHttpVerb() => HttpVerb.Get;
  public string GetRoute() => $"{Route}";
}
