#region Purpose
// SetRoleAgentScopes action: persists the draft agent-scope list for one role.
#endregion

#region Design
// Task 466: same shape as SetRolePermissions. The two drafts stay independent, so saving
// permissions does not post agent scopes. HandleSuccess does not fetch; the page fetches
// only when LastSetRoleAgentScopesSucceeded is true.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

using static SetRoleAgentScopes;

partial class RoleState
{
  public static class SetAgentScopeSelectedActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Guid RoleId { get; }
      public string Scope { get; }
      public bool Selected { get; }

      public Action(Guid roleId, string scope, bool selected)
      {
        RoleId = roleId;
        Scope = scope;
        Selected = selected;
      }
    }

    internal class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        if (!RoleState.DraftAgentScopes.TryGetValue(action.RoleId, out HashSet<string>? set))
        {
          set = [with(StringComparer.Ordinal)];
          RoleState.DraftAgentScopes[action.RoleId] = set;
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

  public static class SetRoleAgentScopesActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Guid RoleId { get; }

      public Action(Guid roleId)
      {
        RoleId = roleId;
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
        RoleState.LastSetRoleAgentScopesSucceeded = false;
        IReadOnlyCollection<string> draft = RoleState.GetDraftAgentScopes(action.RoleId);
        return Task.FromResult<Command?>(new Command
        {
          UserId = Guid.NewGuid(),
          RoleId = action.RoleId,
          Scopes = [.. draft]
        });
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        _ = response;
        _ = cancellationToken;
        RoleState.LastSetRoleAgentScopesSucceeded = true;
        return Task.CompletedTask;
      }
    }
  }
}
