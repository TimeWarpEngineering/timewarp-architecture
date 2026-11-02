#region Purpose
// SetPrincipalAgentScopes action: persists the principal's own agent-scope grants.
#endregion

#region Design
// Task 466: draft toggles stay in state; Save posts SetPrincipalAgentScopes. Success does
// not notify the session — agent scopes are not human permission claims. The page reopens
// the principal only after LastSetPrincipalAgentScopesSucceeded so a failed write keeps
// the draft.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

using static SetPrincipalAgentScopes;

partial class PrincipalState
{
  public static class SetAgentScopeSelectedActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Guid PrincipalId { get; }
      public string Scope { get; }
      public bool Selected { get; }

      public Action(Guid principalId, string scope, bool selected)
      {
        PrincipalId = principalId;
        Scope = scope;
        Selected = selected;
      }
    }

    internal class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        if (!PrincipalState.DraftAgentScopes.TryGetValue(action.PrincipalId, out HashSet<string>? set))
        {
          set = [with(StringComparer.Ordinal)];
          PrincipalState.DraftAgentScopes[action.PrincipalId] = set;
        }

        if (action.Selected)
        {
          set.Add(action.Scope);
        }
        else
        {
          set.Remove(action.Scope);
        }

        return default;
      }
    }
  }

  public static class SetPrincipalAgentScopesActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Guid PrincipalId { get; }

      public Action(Guid principalId)
      {
        PrincipalId = principalId;
      }
    }

    internal class Handler : DefaultApiHandler<Action, Command, Response>
    {
      public Handler(
        IStore store,
        IWebServerApiService webServerApiService,
        ILogger<Handler> logger,
      IPublisher<ClientPipeline> publisher)
        : base(store, webServerApiService, logger, publisher)
      {
      }

      protected override Task<Command?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        PrincipalState.LastSetPrincipalAgentScopesSucceeded = false;
        IReadOnlyCollection<string> draft = PrincipalState.GetDraftAgentScopes(action.PrincipalId);
        return Task.FromResult<Command?>(new Command
        {
          UserId = Guid.NewGuid(),
          PrincipalId = action.PrincipalId,
          Scopes = [.. draft]
        });
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        _ = response;
        _ = cancellationToken;
        PrincipalState.LastSetPrincipalAgentScopesSucceeded = true;
        return Task.CompletedTask;
      }
    }
  }
}
