#region Purpose
// Proto-first gRPC sample: implements the .proto-generated Greeter service, contrasting with the code-first HelloService.
#endregion

#region Design
// Own product slice (task 129 ruling 6c): the proto-first demo's identity matches its .proto
// service, kept visibly distinct from the code-first hello/superhero demo style. Namespace
// adopts the product-slice rule (…Features.Greeters, TWA0009 SliceRoot) even though the base
// class (Greeter.GreeterBase) and message types (HelloRequest/HelloReply) remain in the
// proto-generated TimeWarp.Architecture.GrpcServer namespace — C# does not require an impl's
// namespace to match its base class's. HelloRequest is ambiguous with the code-first
// TimeWarp.Architecture.Features.Hellos.HelloRequest once this file leaves GrpcServer's
// namespace nesting (both are project-global-used in grpc-server), so it stays fully qualified.
#endregion

namespace TimeWarp.Architecture.Features.Greeters;

public class GreeterService : Greeter.GreeterBase
{
  private readonly ILogger Logger;
  public GreeterService(ILogger<GreeterService> logger)
  {
    Logger = logger;
  }

  public override Task<HelloReply> SayHello(TimeWarp.Architecture.GrpcServer.HelloRequest helloRequest, ServerCallContext serverCallContext) =>
    Task.FromResult(new HelloReply { Message = "Hello " + helloRequest.Name });
}
