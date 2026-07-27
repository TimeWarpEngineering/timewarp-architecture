#region Purpose
// Code-first gRPC service contract for the Hello sample; server and clients bind to this shared interface, so no .proto file exists.
#endregion

namespace TimeWarp.Architecture.Features.Hellos;

[ServiceContract]
public interface IHelloService
{
  [OperationContract]
  Task<HelloResponse> SayHelloAsync(HelloRequest helloRequest, ServerCallContext callContext);
}
