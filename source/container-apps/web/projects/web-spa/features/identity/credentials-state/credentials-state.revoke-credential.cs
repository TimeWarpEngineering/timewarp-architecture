#region Purpose
// RevokeCredential: soft-revokes one of the caller's credentials via the authenticated API.
#endregion

#region Design
// DefaultApiHandler + re-fetch on success so the list stays the single source of truth.
// Task 169.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.AspNetCore.Components.Authorization;
using static RevokeCredential;

partial class CredentialsState
{
  internal static class RevokeCredentialActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction
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
        AuthenticationStateProvider authenticationStateProvider
      ) : base(store, webServerApiService, logger, authenticationStateProvider: authenticationStateProvider)
      {
        AuthenticationStateProvider = authenticationStateProvider;
      }

      protected override async Task<Command?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        Guid userId = await ResolveUserIdAsync();
        return new Command { CredentialId = action.CredentialId, UserId = userId };
      }

      protected override async Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        CredentialsState.StatusMessage = "Passkey deleted.";
        CredentialsState.CeremonyError = null;
        await CredentialsState.FetchCredentials(externalCancellationToken: cancellationToken);
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
