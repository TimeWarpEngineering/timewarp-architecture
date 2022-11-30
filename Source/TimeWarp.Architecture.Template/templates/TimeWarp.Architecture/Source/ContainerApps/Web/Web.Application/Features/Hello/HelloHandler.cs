namespace TimeWarp.Architecture.Features.Hello;

public class HelloHandler : IRequestHandler<HelloRequest, HelloResponse>
{
  public Task<HelloResponse> Handle
  (
    HelloRequest aHelloRequest,
    CancellationToken aCancellationToken
  )
  {
    // TODO implement code here that formats and sends data to your favorite Analytics tool

    var helloResponse = new HelloResponse();
    return Task.FromResult(helloResponse);
  }
}
