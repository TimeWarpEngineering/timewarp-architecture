namespace TimeWarp.Architecture.Features.Hello;

using Grpc.Core;
using System.Threading.Tasks;
using TimeWarp.Architecture.Features.Hellos;

public class HelloService : IHelloService
{
  public Task<HelloResponse> SayHelloAsync(HelloRequest aHelloRequest, ServerCallContext aCallContext) =>
    Task.FromResult(new HelloResponse { Message = $"Hello {aHelloRequest.Name}" });
}

