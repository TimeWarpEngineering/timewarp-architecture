#region Purpose
// FetchSuperhero action: loads sample heroes into state, exercising the gRPC transport path.
#endregion

#region Design
// Counterpart to the REST-based weather fetch — this feature exists to demonstrate the grpc
// feature flag, so the handler stays deliberately minimal (hardcoded count, no error branch).
// The service is resolved through SuperheroGrpcServiceProvider at handle time because channel
// creation is async and render-mode dependent; the client cannot be constructor-injected.
// The list is cleared and refilled in place (not replaced) so the read-only projection in the
// main partial keeps pointing at the same backing list.
#endregion

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
