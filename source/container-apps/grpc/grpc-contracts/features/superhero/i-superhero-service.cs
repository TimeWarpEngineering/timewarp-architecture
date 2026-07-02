#region Purpose
// Code-first gRPC contract for the superhero demo, shared by the server and its .NET clients.
#endregion

#region Design
// Code-first (protobuf-net.Grpc) instead of proto-first: .NET consumers reference this assembly directly,
// eliminating .proto codegen; ProtobufGenerationHostedService can emit a .proto from it for non-.NET interop.
// CallContext (not ServerCallContext) keeps the same signature usable on both client and server.
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
