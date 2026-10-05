#region Purpose
// RenameCredential: sets the user nickname on one of the caller's credentials via the authenticated API.
#endregion

#region Design
// DefaultApiHandler owns transport + problem-details → shell message bar. HandleSuccess clears
// the pending "name your new passkey" prompt when it was for this credential and publishes the
// success sentence to the shell's single notification region (NotificationState, task 247
// rule 1) — no page-local status bar for rename. Task 248-001 + 247.
// Task 279: Visibility Both — the server offers this action (GetCredentials.Offers) and Settings /
// Passkeys run the offer through the catalog (CommandPaletteRunner.RunContextualAsync), which refuses
// any entry that is not human-visible; agents still call it directly. The runner then runs
// FetchCredentials (the offer row's follow-up), so the list and its offers stay the single source
// of truth.
// Task 280: Name = OfferedActionNames constant shared with the server's RenameCredentialOffer record, so a
// rename of this action set cannot change the offered name; TWA0029/TWA0030 check the pairing.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.AspNetCore.Components.Authorization;
using static RenameCredential;

partial class CredentialsState
{
  public static class RenameCredentialActionSet
  {
    [CatalogAction
    (
      Name = OfferedActionNames.RenameCredential,
      Description = "Set the nickname of one of the signed-in account's credentials.",
      Permissions = [PermissionIds.CredentialManageSelf],
      Visibility = ActionVisibility.Both
    )]
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
