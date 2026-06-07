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
