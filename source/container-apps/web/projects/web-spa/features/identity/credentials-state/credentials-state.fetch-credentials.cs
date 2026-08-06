#region Purpose
// FetchCredentials: loads GetCredentials into CredentialsState for Settings.
#endregion

#region Design
// DefaultApiHandler owns transport + toast-on-error. UserId is client FluentValidation / mock
// signal only (server uses session principal). Prefer signed-in claim; Guid.NewGuid() fallback
// matches RoleState when claim resolution fails (AuthApiRequestValidator needs non-empty UserId).
// Task 169.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.AspNetCore.Components.Authorization;
using static GetCredentials;

partial class CredentialsState
{
  internal static class FetchCredentialsActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction
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
        ISender sender,
        ILogger<Handler> logger,
        AuthenticationStateProvider authenticationStateProvider
      ) : base(store, webServerApiService, sender, logger, authenticationStateProvider: authenticationStateProvider)
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
        CredentialsState.CeremonyError = null;
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
