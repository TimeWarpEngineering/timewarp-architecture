namespace TimeWarp.Architecture.Features.Superheros;

internal partial class SuperheroState
{
  public class FetchSuperheroHandler : BaseHandler<FetchSuperheroAction>
  {
    private readonly ISuperheroService SuperheroService;
    public FetchSuperheroHandler(IStore aStore, ISuperheroService aSuperheroService) : base(aStore)
    {
      SuperheroService = aSuperheroService;
    }

    public override async Task Handle
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
    }
  }
}
