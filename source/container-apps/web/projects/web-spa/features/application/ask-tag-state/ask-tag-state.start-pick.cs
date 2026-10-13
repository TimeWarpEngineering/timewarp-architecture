#region Purpose
// StartPick: the Ask Tag button. Turn on pick mode and attach the element the person chooses.
#endregion

#region Design
// The handler waits for ask-tag.ts (AskTagJsModule.PickAsync) while the person points or uses the
// keyboard; the panel reads IsPicking to show "Picking" and the Cancel button while it waits (the
// action is not tracked: a tracked action is the app-wide busy signal, and the page must not look
// busy while the person points at it); because the handler stays open, it re-renders the
// AskTagState subscribers itself once IsPicking is on. Route is the page the pick ran on (the panel passes PageAgentRoute's path). A pick applies
// only while its PickGeneration is current: CancelPick, or a newer StartPick, makes the old answer
// stale. Cancel (Esc, a click on nothing, or a dropped circuit) attaches nothing.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using TimeWarp.Architecture.Services;

partial class AskTagState
{
  public static class StartPickActionSet
  {
    public sealed class Action : IBaseAction
    {
      public string Route { get; }

      public Action(string route)
      {
        Route = route ?? "";
      }
    }

    internal sealed class Handler
    (
      IStore store,
      Subscriptions subscriptions,
      IJSRuntime jsRuntime,
      AuthenticationStateProvider authenticationStateProvider,
      IAuthorizationService authorizationService
    ) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        int pick = ++AskTagState.PickGeneration;
        AskTagState.IsPicking = true;
        // The handler stays open until the person answers, so subscribers are told now.
        subscriptions.ReRenderSubscribers<AskTagState>();
        AskTagCandidate? candidate = await AskTagJsModule.PickAsync(jsRuntime, cancellationToken);
        if (AskTagState.PickGeneration != pick)
        {
          return;
        }

        AskTagState.IsPicking = false;
        if (candidate is null)
        {
          return;
        }

        await AttachAsync
        (
          () => AskTagState,
          AgentSurfaceState.ConversationGeneration,
          candidate,
          action.Route,
          authenticationStateProvider,
          authorizationService
        );
      }
    }
  }
}
