#region Purpose
// Code-first gRPC contract for the superhero demo, shared by the server and its .NET clients.
#endregion

#region Design
// Code-first (protobuf-net.Grpc) instead of proto-first: .NET consumers reference this assembly directly,
// eliminating .proto codegen; ProtobufGenerationHostedService can emit a .proto from it for non-.NET interop.
// CallContext (not ServerCallContext) keeps the same signature usable on both client and server.
// Stays -contracts.cs, NOT -application.cs (task 129 stage 2, deviates from the sibling
// i-hello-service seam move): web-spa (WASM client) references ISuperheroService directly via
// SuperheroGrpcServiceProvider, and its ProjectReference is to grpc-contracts.csproj only.
// Moving this interface's compilation unit to grpc-application would break that reference (or
// force web-spa to add a grpc-application ProjectReference, dragging an application-layer
// assembly into the WASM client's dependency graph) — a consequence the seam-interface ruling
// never examined for gRPC's cross-app-consumed contracts. IHelloService has no such consumer and
// took the seam move cleanly; see i-hello-service-application.cs.
#endregion

namespace TimeWarp.Architecture.Features.Superheros;

[ServiceContract]
public interface ISuperheroService
{
  [OperationContract]
  Task<SuperheroResponse> GetSuperheroAsync
  (
    SuperheroRequest superheroRequest,
    CallContext callContext = default
  );
}
