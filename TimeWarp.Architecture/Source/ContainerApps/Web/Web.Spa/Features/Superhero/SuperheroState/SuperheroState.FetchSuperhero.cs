namespace TimeWarp.Architecture.Features.Superheros;

partial class SuperheroState
{
  public static class FetchSuperheroActionSet
  {
    public sealed class Action : IBaseAction { }

    internal sealed class Handler : BaseHandler<Action>
    {
      private readonly SuperheroGrpcServiceProvider SuperheroGrpcServiceProvider;
      public Handler
      (
        IStore store,
        SuperheroGrpcServiceProvider superheroGrpcServiceProvider
      ) : base(store)
      {
        SuperheroGrpcServiceProvider = superheroGrpcServiceProvider;
      }
      public override async Task Handle(Action action, CancellationToken cancellationToken)
      {
        SuperheroState.SuperheroList.Clear();
        var getSuperheroRequest = new SuperheroRequest { NumberOfHeros = 5 };

        ISuperheroService superheroService = await SuperheroGrpcServiceProvider.GetGrpcServiceAsync(cancellationToken);

        SuperheroResponse getSuperheroResponse =
          await superheroService.GetSuperheroAsync(getSuperheroRequest);

        SuperheroState.SuperheroList.AddRange(getSuperheroResponse.Superheros);
      }
    }
  }
}
