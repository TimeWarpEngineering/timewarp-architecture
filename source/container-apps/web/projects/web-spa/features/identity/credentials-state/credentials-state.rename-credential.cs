#region Purpose
// RenameCredential: sets the user nickname on one of the caller's credentials via the authenticated API.
#endregion

#region Design
// DefaultApiHandler owns transport + problem-details → shell message bar. HandleSuccess clears
// the pending "name your new passkey" prompt when it was for this credential and writes the
// success sentence straight into ToastNotificationState (the shell's single notification region,
// task 247 rule 1) — no page-local status bar for rename. The handler mutates that state and asks
// Subscriptions to re-render it, mirroring ProblemDetailsNotificationHandler; it does not Send
// (TWS0002). Callers sequence FetchCredentials afterwards so the list stays the single source of
// truth. Task 248-001.
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
      private readonly Subscriptions Subscriptions;
      private Guid RenamedCredentialId;

      public Handler
      (
        IStore store,
        IWebServerApiService webServerApiService,
        ILogger<Handler> logger,
        IPublisher<ClientPipeline> publisher,
        AuthenticationStateProvider authenticationStateProvider,
        Subscriptions subscriptions
      ) : base(store, webServerApiService, logger, publisher, authenticationStateProvider: authenticationStateProvider)
      {
        AuthenticationStateProvider = authenticationStateProvider;
        Subscriptions = subscriptions;
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
        _ = cancellationToken;
        if (CredentialsState.PendingNicknameCredentialId == RenamedCredentialId)
        {
          CredentialsState.PendingNicknameCredentialId = null;
          CredentialsState.PendingNicknameDefault = null;
        }

        CredentialsState.CeremonyError = null;
        Store.GetState<ToastNotificationState>().AddMessage(MessageBarIntent.Success, "Nickname saved.", body: null);
        Subscriptions.ReRenderSubscribers<ToastNotificationState>();
        return Task.CompletedTask;
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
