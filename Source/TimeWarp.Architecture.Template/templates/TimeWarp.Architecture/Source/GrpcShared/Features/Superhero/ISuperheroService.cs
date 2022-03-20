namespace TimeWarp.Architecture.Features.Superheros;

using ProtoBuf.Grpc;
using System.ServiceModel;
using System.Threading.Tasks;

[ServiceContract]
public interface ISuperheroService
{
  [OperationContract]
  Task<SuperheroResponse> GetSuperheroAsync
  (
    SuperheroRequest aSuperheroRequest,
    CallContext aCallContext = default
  );
}
