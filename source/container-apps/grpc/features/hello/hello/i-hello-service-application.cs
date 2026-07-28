#region Purpose
// Code-first gRPC service contract for the Hello sample; server and clients bind to this shared interface, so no .proto file exists.
#endregion

#region Design
// Seam-interface pattern (task 129 ruling 6a, identity-host precedent): -application.cs beside
// its -server.cs implementation (hello-service-server.cs), rather than -contracts.cs alongside
// the DTOs. Safe here because IHelloService has no consumer outside the grpc family — unlike
// ISuperheroService (see i-superhero-service-contracts.cs), nothing references it through a
// project reference scoped to grpc-contracts only. Needs grpc-application/global-usings.cs to
// carry Grpc.Core + System.ServiceModel (global usings are per-project, not inherited through
// ProjectReference) for [ServiceContract]/[OperationContract]/ServerCallContext to resolve here.
#endregion

namespace TimeWarp.Architecture.Features.Hellos;

[ServiceContract]
public interface IHelloService
{
  [OperationContract]
  Task<HelloResponse> SayHelloAsync(HelloRequest helloRequest, ServerCallContext callContext);
}
