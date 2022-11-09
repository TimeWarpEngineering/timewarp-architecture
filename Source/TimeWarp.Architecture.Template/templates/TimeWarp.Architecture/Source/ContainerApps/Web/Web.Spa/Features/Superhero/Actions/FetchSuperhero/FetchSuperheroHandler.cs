namespace TimeWarp.Architecture.Features.Superheros;

using BlazorState;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using TimeWarp.Architecture.Features;

internal partial class SuperheroState
{
  public class FetchSuperheroHandler : BaseHandler<FetchSuperheroAction>
  {
    private readonly ISuperheroService SuperheroService;
    public FetchSuperheroHandler(IStore aStore, ISuperheroService aSuperheroService) : base(aStore)
    {
      SuperheroService = aSuperheroService;
    }

    public override async Task<Unit> Handle
    (
      FetchSuperheroAction aFetchSuperheroAction,
      CancellationToken aCancellationToken
    )
    {
      SuperheroState._Superheros.Clear();
      var getSuperheroRequest = new SuperheroRequest { NumberOfHeros = 5 };

      SuperheroResponse getSuperheroResponse =
        await SuperheroService.GetSuperheroAsync(getSuperheroRequest);

      SuperheroState._Superheros.AddRange(getSuperheroResponse.Superheros);

      return Unit.Value;
    }
  }
}
