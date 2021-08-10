namespace TimeWarp.Blazor.Features.SuperheroGrpc
{
  using ProtoBuf.Grpc;
  using System.ServiceModel;
  using System.Threading.Tasks;

  [ServiceContract]
  public interface ISuperheroService
  {
    [OperationContract]
    Task<SuperheroGrpcResponse> GetSuperheroAsync
    (
      SuperheroGrpcRequest aSuperheroGrpcRequest,
      CallContext aCallContext = default
    );
  }
}
