namespace TimeWarp.Architecture.Features.Superheros;
internal partial class SuperheroState
{
  public static class FetchSuperhero
  {

    public record Action : BaseAction { }

    public class Handler : BaseHandler<Action>
    {
      private readonly ISuperheroService SuperheroService;
      public Handler(IStore store, ISuperheroService superheroService) : base(store)
      {
        SuperheroService = superheroService;
      }

      public override async Task Handle(Action action, CancellationToken aCancellationToken)
      {
        SuperheroState._Superheros.Clear();
        var getSuperheroRequest = new SuperheroRequest { NumberOfHeros = 5 };

        SuperheroResponse getSuperheroResponse =
          await SuperheroService.GetSuperheroAsync(getSuperheroRequest);

        SuperheroState._Superheros.AddRange(getSuperheroResponse.Superheros);
      }
    }
  }
}
