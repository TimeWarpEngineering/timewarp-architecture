#region Purpose
// Server implementation of the code-first IHelloService gRPC contract — the sample showing how Grpc.Server fulfills contracts defined in Grpc.Contracts.
#endregion

namespace TimeWarp.Architecture.Features.Hellos;

public class HelloService : IHelloService
{
  public Task<HelloResponse> SayHelloAsync(Hellos.HelloRequest helloRequest, ServerCallContext callContext) =>
    Task.FromResult(new HelloResponse { Message = $"Hello {helloRequest.Name}" });
}

