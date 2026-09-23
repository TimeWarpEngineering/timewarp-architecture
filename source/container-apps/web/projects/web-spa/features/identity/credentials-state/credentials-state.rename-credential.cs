#region Purpose
// RenameCredential: sets the user nickname on one of the caller's credentials via the authenticated API.
#endregion

#region Design
// DefaultApiHandler owns transport + problem-details → shell message bar. HandleSuccess clears
// the pending "name your new passkey" prompt when it was for this credential and publishes the
// success sentence to the shell's single notification region (NotificationState, task 247
// rule 1) — no page-local status bar for rename. Callers sequence FetchCredentials afterwards so
// the list stays the single source of truth. Task 248-001 + 247.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.AspNetCore.Components.Authorization;
using static RenameCredential;

partial class CredentialsState
{
  public static class RenameCredentialActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Action(Guid credentialId, string nickname)
      {
        CredentialId = credentialId;
        Nickname = nickname;
      }

      public Guid CredentialId { get; }
      public string Nickname { get; }
    }

    internal sealed class Handler : DefaultApiHandler<Action, Command, Response>
    {
      private readonly AuthenticationStateProvider AuthenticationStateProvider;
      private Guid RenamedCredentialId;

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
        RenamedCredentialId = action.CredentialId;
        Guid userId = await ResolveUserIdAsync();
        return new Command { CredentialId = action.CredentialId, UserId = userId, Nickname = action.Nickname };
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        _ = response;
        if (CredentialsState.PendingNicknameCredentialId == RenamedCredentialId)
        {
          CredentialsState.PendingNicknameCredentialId = null;
          CredentialsState.PendingNicknameDefault = null;
          CredentialsState.PendingNicknameOwnedByPrompt = false;
        }

        return Publisher.Publish(new OutcomeNotification(MessageBarIntent.Success, "Nickname saved."), cancellationToken);
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
