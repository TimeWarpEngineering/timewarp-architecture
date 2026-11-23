#region Purpose
// SetPrincipalRoles action: persists the role list the page built for one principal.
#endregion

#region Design
// The page owns the checkbox working copy and passes RoleIds on the command. This handler
// stamps UserId and sends that list. It does not read a store draft.
// HandleSuccess does not fetch. The Set response echoes *stored* roles only — the page
// recopies from OpenPrincipal, which returns effective roles (roleless human → Member;
// roleless agent → no role; bootstrap unions Admin+Member; review M1).
// LastSetPrincipalRolesSucceeded is set false in GetRequest and true in HandleSuccess so
// the page reopens only after a successful write. NotifySessionChanged is not an
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
  public static class SetPrincipalRolesActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Command Command { get; }

      public Action(Command command)
      {
        Command = command;
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
      IPublisher<ClientPipeline> publisher,
        AuthenticationStateProvider authenticationStateProvider)
        : base(store, webServerApiService, logger, publisher)
      {
        AuthenticationStateProvider = authenticationStateProvider;
      }

      protected override Task<Command?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        ActivePrincipalId = action.Command.PrincipalId;
        PrincipalState.LastSetPrincipalRolesSucceeded = false;
        return Task.FromResult<Command?>(new Command
        {
          UserId = Guid.NewGuid(),
          PrincipalId = action.Command.PrincipalId,
          RoleIds = [.. action.Command.RoleIds]
        });
      }

      protected override async Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        _ = response;
        PrincipalState.LastSetPrincipalRolesSucceeded = true;
        if (AuthenticationStateProvider is IdentitySessionAuthenticationStateProvider identitySession)
        {
          // One read to see whether the edited principal is the signed-in user.
#pragma warning disable BL0013
          AuthenticationState authState =
            await AuthenticationStateProvider.GetAuthenticationStateAsync();
#pragma warning restore BL0013
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
