#region Purpose
// FetchCredentials: loads GetCredentials into CredentialsState for Settings.
#endregion

#region Design
// DefaultApiHandler owns transport + toast-on-error. The query does not name the caller; the
// server reads the session principal (task 458-008).
// AuthenticationStateListener fetches on sign-in so RFC 219 D8 AddPasskeyPrompt has a snapshot
// without visiting Settings. Task 169 + 219-003.
// HandleSuccess stores GetCredentials.Response.CanLinkMicrosoft365 beside the credentials (one
// snapshot, task 282); the per-row flags ride on the summaries. Cataloged with Visibility Agent:
// parameter-free, so an agent may read the same list and flags; humans load it with their page.
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
      Description = "Refresh the signed-in account's credentials and which credential actions apply now.",
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

      public Handler
      (
        IStore store,
        IWebServerApiService webServerApiService,
        ILogger<Handler> logger,
      IPublisher<ClientPipeline> publisher,
        AuthenticationStateProvider authenticationStateProvider
      ) : base(store, webServerApiService, logger, publisher, authenticationStateProvider: authenticationStateProvider)
      { }

      protected override Task<Query?> GetRequest(Action action, CancellationToken cancellationToken) =>
        Task.FromResult<Query?>(new Query { IncludeRevoked = action.IncludeRevoked });

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        CredentialsState.CredentialsList = [.. response.Credentials];
        CredentialsState.CanLinkMicrosoft365 = response.CanLinkMicrosoft365;
        CredentialsState.CeremonyFailed = false;
        return Task.CompletedTask;
      }
    }
  }
}
