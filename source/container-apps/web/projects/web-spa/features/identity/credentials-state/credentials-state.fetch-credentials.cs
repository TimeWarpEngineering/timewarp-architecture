#region Purpose
// FetchCredentials: loads GetCredentials into CredentialsState for Settings.
#endregion

#region Design
// DefaultApiHandler owns transport + toast-on-error. UserId is client FluentValidation / mock
// signal only (server uses session principal). Prefer signed-in claim; Guid.NewGuid() fallback
// matches RoleState when claim resolution fails (AuthApiRequestValidator needs non-empty UserId).
// AuthenticationStateListener fetches on sign-in so RFC 219 D8 AddPasskeyPrompt has a snapshot
// without visiting Settings. Task 169 + 219-003.
// Task 279: HandleSuccess stores the server's Offers beside the credentials (one snapshot). Cataloged
// (Visibility Agent, parameterless) because it is the follow-up of every offered credential action:
// the runner that executed an offer refreshes through the catalog, so no handler dispatches another
// action (TWS0002). Agent visibility keeps it out of the static Ctrl-K roster; an agent may call it to
// read the same list and offers.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.AspNetCore.Components.Authorization;
using static GetCredentials;

partial class CredentialsState
{
  public static class FetchCredentialsActionSet
  {
    public const string CatalogName = "Credentials.FetchCredentials";

    [CatalogAction
    (
      Description = "Refresh the signed-in account's credentials and the actions the server offers for them.",
      Permissions = [PermissionIds.CredentialManageSelf],
      Visibility = ActionVisibility.Agent
    )]
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Action(bool includeRevoked = false)
      {
        IncludeRevoked = includeRevoked;
      }

      public bool IncludeRevoked { get; }
    }

    internal sealed class Handler : DefaultApiHandler<Action, Query, Response>
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

      protected override async Task<Query?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        Guid userId = await ResolveUserIdAsync();
        return new Query { IncludeRevoked = action.IncludeRevoked, UserId = userId };
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        CredentialsState.CredentialsList = [.. response.Credentials];
        CredentialsState.OffersList = [.. response.Offers.Select(CredentialOffer.From)];
        CredentialsState.CeremonyFailed = false;
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
