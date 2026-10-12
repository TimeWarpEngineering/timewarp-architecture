#region Purpose
// FetchSuperhero action: loads sample heroes into state, exercising the gRPC transport path.
#endregion

#region Design
// Counterpart to the REST-based weather fetch — this feature exists to demonstrate the grpc
// feature flag, so the handler stays deliberately minimal (hardcoded count, no error branch).
// The service is resolved through SuperheroGrpcServiceProvider at handle time: the channel is built
// lazily from the host's grpc-server named HttpClient, whose BaseAddress differs per render host.
// The list is cleared and refilled in place (not replaced) so the read-only projection in the
// main partial keeps pointing at the same backing list.
// [TrackAction] is the loading signal — Superheros is never null (empty list until fetch).
#endregion

namespace TimeWarp.Architecture.Features.Superheros;

partial class SuperheroState
{
  public static class FetchSuperheroActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction;

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
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        SuperheroState.SuperheroList.Clear();
        var getSuperheroRequest = new SuperheroRequest { NumberOfHeros = 5 };

        ISuperheroService superheroService = SuperheroGrpcServiceProvider.GetGrpcService();

        SuperheroResponse getSuperheroResponse =
          await superheroService.GetSuperheroAsync(getSuperheroRequest);

        SuperheroState.SuperheroList.AddRange(getSuperheroResponse.Superheros);
      }
    }
  }
}
