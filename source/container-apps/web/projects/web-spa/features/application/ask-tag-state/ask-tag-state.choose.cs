#region Purpose
// Choose: attach a described element as a composer chip.
#endregion

#region Design
// The same Attach StartPick runs once the browser answers, as its own action so the chip rules
// (caps, the reference check, the per-conversation list) are testable without a browser.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AskTagState
{
  public static class ChooseActionSet
  {
    public sealed class Action : IBaseAction
    {
      public AskTagCandidate Candidate { get; }

      public string Route { get; }

      public Action(AskTagCandidate candidate, string route)
      {
        Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
        Route = route ?? "";
      }
    }

    internal sealed class Handler
    (
      IStore store,
      AuthenticationStateProvider authenticationStateProvider,
      IAuthorizationService authorizationService
    ) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        await AttachAsync
        (
          () => AskTagState,
          AgentSurfaceState.ConversationGeneration,
          action.Candidate,
          action.Route,
          authenticationStateProvider,
          authorizationService
        );
      }
    }
  }
}
