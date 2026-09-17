#region Purpose
// SetPrincipalRoles action: persists draft role multi-select for one principal.
#endregion

#region Design
// HandleSuccess does not fetch. The Set response echoes *stored* roles only — patching
// drafts from it desyncs virtual grants (empty store → Member; bootstrap unions
// Admin+Member; review M1). PrincipalsPage sequences SetPrincipalRoles then FetchPrincipals
// so the list re-seeds from ListPrincipals effective roles. NotifySessionChanged is not an
// action dispatch (calls the auth state provider directly) and stays here so AuthorizeView /
// nav re-run developer.access when the edited principal is the signed-in identity-session
// user (WASM: re-project GetCurrentSession permission claims; Server circuit:
// PermissionRequirement re-expands — evaluator must not stick a circuit-lifetime cache,
// task 189).
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

using Microsoft.AspNetCore.Components.Authorization;
using TimeWarp.Architecture.Services;
using static SetPrincipalRoles;

partial class PrincipalState
{
  public static class SetRoleSelectedActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction
    {
      public Guid PrincipalId { get; }
      public Guid RoleId { get; }
      public bool Selected { get; }

      public Action(Guid principalId, Guid roleId, bool selected)
      {
        PrincipalId = principalId;
        RoleId = roleId;
        Selected = selected;
      }
    }

    internal class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        if (!PrincipalState.DraftRoleIds.TryGetValue(action.PrincipalId, out HashSet<Guid>? set))
        {
          set = [];
          PrincipalState.DraftRoleIds[action.PrincipalId] = set;
        }

        if (action.Selected)
        {
          set.Add(action.RoleId);
        }
        else
        {
          set.Remove(action.RoleId);
        }

        return Task.CompletedTask;
      }
    }
  }

  public static class SetPrincipalRolesActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction
    {
      public Guid PrincipalId { get; }

      public Action(Guid principalId)
      {
        PrincipalId = principalId;
      }
    }

    internal class Handler : DefaultApiHandler<Action, Command, Response>
    {
      private readonly AuthenticationStateProvider AuthenticationStateProvider;
      private Guid ActivePrincipalId;

      public Handler(
        IStore store,
        IWebServerApiService webServerApiService,
        ILogger<Handler> logger,
        AuthenticationStateProvider authenticationStateProvider)
        : base(store, webServerApiService, logger)
      {
        AuthenticationStateProvider = authenticationStateProvider;
      }

      protected override Task<Command?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        ActivePrincipalId = action.PrincipalId;
        IReadOnlyCollection<Guid> draft = PrincipalState.GetDraftRoleIds(action.PrincipalId);
        return Task.FromResult<Command?>(new Command
        {
          UserId = Guid.NewGuid(),
          PrincipalId = action.PrincipalId,
          RoleIds = [.. draft]
        });
      }

      protected override async Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        _ = response;
        if (AuthenticationStateProvider is IdentitySessionAuthenticationStateProvider identitySession)
        {
          AuthenticationState authState =
            await AuthenticationStateProvider.GetAuthenticationStateAsync();
          string? currentId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? authState.User.FindFirst("timewarp:principal_id")?.Value;
          if (Guid.TryParse(currentId, out Guid currentPrincipal)
            && currentPrincipal == ActivePrincipalId)
          {
            identitySession.NotifySessionChanged();
          }
        }
      }
    }
  }
}
