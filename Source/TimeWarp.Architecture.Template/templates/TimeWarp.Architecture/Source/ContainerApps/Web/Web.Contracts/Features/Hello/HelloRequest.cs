namespace TimeWarp.Architecture.Features.Hello;

[RouteMixin("Hello", HttpVerb.Get)]
public partial record HelloRequest : BaseRequest, IApiRequest, IRequest<HelloResponse> { }

// public record HelloRequest : BaseRequest, IApiRequest, IRequest<HelloResponse>
// {
//   public const string Route = "Hello";
//
//   public HttpVerb GetHttpVerb() => HttpVerb.Get;
//   public string GetRoute() => $"{Route}";
// }
