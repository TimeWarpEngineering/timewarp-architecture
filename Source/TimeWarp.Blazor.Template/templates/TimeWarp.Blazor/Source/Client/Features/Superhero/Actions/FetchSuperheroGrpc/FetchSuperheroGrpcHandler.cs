namespace TimeWarp.Blazor.Features.Superheros
{
  using BlazorState;
  using MediatR;
  using System.Threading;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.Bases;
  using TimeWarp.Blazor.Features.SuperheroGrpc;

  internal partial class SuperheroState
  {
    public class FetchSuperheroGrpcHandler : BaseHandler<FetchSuperheroGrpcAction>
    {
      private readonly ISuperheroService SuperheroService;
      public FetchSuperheroGrpcHandler(IStore aStore, ISuperheroService aSuperheroService) : base(aStore)
      {
        SuperheroService = aSuperheroService;
      }

      public override async Task<Unit> Handle
      (
        FetchSuperheroGrpcAction aFetchSuperheroGrpcAction,
        CancellationToken aCancellationToken
      )
      {
        SuperheroState._Superheros.Clear();
        var getSuperheroGrpcRequest = new SuperheroGrpcRequest { NumberOfHero = 5};
        SuperheroGrpcResponse getSuperheroGrpcResponse =
          await SuperheroService.GetSuperheroAsync(getSuperheroGrpcRequest);

        foreach (SuperheroGrpcDto superhero in getSuperheroGrpcResponse.SuperherosGrpc)
        {
          SuperheroState._Superheros.Add(superhero);
        }
        return Unit.Value;
      }
    }
  }
}
