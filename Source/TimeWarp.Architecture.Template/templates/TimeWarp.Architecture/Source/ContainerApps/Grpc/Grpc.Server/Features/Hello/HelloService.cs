namespace TimeWarp.Architecture.Features.Hello;

public class HelloService : IHelloService
{
  public Task<HelloResponse> SayHelloAsync(Hellos.HelloRequest aHelloRequest, ServerCallContext aCallContext) =>
    Task.FromResult(new HelloResponse { Message = $"Hello {aHelloRequest.Name}" });
}

