#region Purpose
// RevokeCredential: soft-revokes one of the caller's credentials via the authenticated API.
#endregion

#region Design
// DefaultApiHandler owns transport + ProblemDetailsNotification on error. HandleSuccess
// publishes the "Credential revoked." outcome to the shell region — the action serves
// passkeys, agent keys and Entra unlink alike (task 246 vocabulary). Task 169 + 246 + 247.
// Visibility Agent (task 282): this action needs a credential id, so humans reach it from the row
// button on Settings / Passkeys (shown while CredentialSummary.CanRevoke holds), which dispatches it
// directly; Ctrl-K lists only navigation and parameter-free commands. Agents call it through the
// catalog. Default derived catalog name — no other side spells it.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.AspNetCore.Components.Authorization;
using static RevokeCredential;

partial class CredentialsState
{
  public static class RevokeCredentialActionSet
  {
    [CatalogAction
    (
      Description = "Revoke one of the signed-in account's credentials by id.",
      Permissions = [PermissionIds.CredentialManageSelf],
      Visibility = ActionVisibility.Agent
    )]
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Action(Guid credentialId)
      {
        CredentialId = credentialId;
      }

      public Guid CredentialId { get; }
    }

    internal sealed class Handler : DefaultApiHandler<Action, Command, Response>
    {
      private readonly AuthenticationStateProvider AuthenticationStateProvider;

      public Handler
      (
        IStore store,
        IWebServerApiService webServerApiService,
        ILogger<Handler> logger,
      IPublisher<ClientPipeline> publisher,
        AuthenticationStateProvider authenticationStateProvider
      ) : base(store, webServerApiService, logger, publisher, authenticationStateProvider: authenticationStateProvider)
      {
        AuthenticationStateProvider = authenticationStateProvider;
      }

      protected override async Task<Command?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        Guid userId = await ResolveUserIdAsync();
        return new Command { CredentialId = action.CredentialId, UserId = userId };
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        _ = response;
        CredentialsState.CeremonyFailed = false;
        return Publisher.Publish
        (
          new OutcomeNotification(MessageBarIntent.Success, "Credential revoked."),
          cancellationToken
        );
      }

      private async Task<Guid> ResolveUserIdAsync()
      {
        try
        {
          return await AuthenticationStateProvider.GetUserIdAsync();
        }
        catch (InvalidOperationException)
        {
          return Guid.NewGuid();
        }
      }
    }
  }
}
