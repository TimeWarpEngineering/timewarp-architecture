namespace TimeWarp.Architecture.Features.Hellos;

public class HelloService : IHelloService
{
  public Task<HelloResponse> SayHelloAsync(Hellos.HelloRequest helloRequest, ServerCallContext callContext) =>
    Task.FromResult(new HelloResponse { Message = $"Hello {helloRequest.Name}" });
}

