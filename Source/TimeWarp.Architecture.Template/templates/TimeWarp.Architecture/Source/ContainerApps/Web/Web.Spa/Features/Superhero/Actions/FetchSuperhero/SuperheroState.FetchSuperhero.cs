﻿namespace TimeWarp.Architecture.Features.Superheros;

internal partial class SuperheroState
{
  public static class FetchSuperhero
  {
    public sealed class Action : IBaseAction { }

    [UsedImplicitly]
    public sealed class Handler
    (
      IStore store,
      ISuperheroService superheroService
    ) : BaseHandler<Action>(store)
    {
      public override async Task Handle(Action action, CancellationToken aCancellationToken)
      {
        SuperheroState.SuperheroList.Clear();
        var getSuperheroRequest = new SuperheroRequest { NumberOfHeros = 5 };

        SuperheroResponse getSuperheroResponse =
          await superheroService.GetSuperheroAsync(getSuperheroRequest);

        SuperheroState.SuperheroList.AddRange(getSuperheroResponse.Superheros);
      }
    }
  }
}
