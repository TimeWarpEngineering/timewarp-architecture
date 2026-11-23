#region Purpose
// SetRoleAgentScopes: the local agent-scope toggle and the hooks of the generated save action.
#endregion

#region Design
// Task 466: same shape as SetRolePermissions. The two drafts stay independent, so saving
// permissions does not post agent scopes. The save is generated from [ContractState] (task 483)
// with SetRoleAgentScopes in DraftRequests, so BuildSetRoleAgentScopesRequest is required and fills
// Scopes from the draft. Success does not fetch; the page fetches only when
// LastSetRoleAgentScopesSucceeded is true.
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

  private partial void BuildSetRoleAgentScopesRequest(Command request)
  {
    LastSetRoleAgentScopesSucceeded = false;
    request.Scopes = [.. GetDraftAgentScopes(request.RoleId)];
  }

  partial void OnSetRoleAgentScopesSucceeded(Response response) => LastSetRoleAgentScopesSucceeded = true;
}
