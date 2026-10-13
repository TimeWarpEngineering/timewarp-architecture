#region Purpose
// OpenPrincipal action: loads one principal (status, roles, gated profile) for the detail page.
#endregion

#region Design
// Task 328. Opening a different principal first clears Current and LoadError, so the previous
// principal's profile and role editor never show under the new id while it loads or after it
// fails. A 404 clears Current and sets a non-revealing LoadError, then still publishes the
// problem. On success the role draft for that principal is seeded from the detail's effective
// RoleIds (replacing any unsaved edits, which is the intent after a save or a reload).
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

using static GetPrincipal;

partial class PrincipalState
{
  public static class OpenPrincipalActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Action(Guid principalId)
      {
        PrincipalId = principalId;
      }

      public Guid PrincipalId { get; }
    }

    internal class Handler
    (
      IStore store,
      IWebServerApiService webServerApiService,
      ILogger<Handler> logger,
      IPublisher<ClientPipeline> publisher
    ) : DefaultApiHandler<Action, Query, Response>(store, webServerApiService, logger, publisher)
    {
      protected override Task<Query?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        if (PrincipalState.Current?.PrincipalId.Value != action.PrincipalId)
        {
          PrincipalState.Current = null;
          PrincipalState.LoadError = null;
        }

        return Task.FromResult<Query?>(new Query { PrincipalId = action.PrincipalId });
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        PrincipalState.Current = response.Principal;
        PrincipalState.LoadError = null;
        PrincipalState.DraftRoleIds[response.Principal.PrincipalId.Value] = [.. response.Principal.RoleIds];
        return Task.CompletedTask;
      }

      protected override async Task HandleError(SharedProblemDetails problemDetails, CancellationToken cancellationToken)
      {
        if (problemDetails.Status == 404)
        {
          PrincipalState.Current = null;
          PrincipalState.LoadError = "This principal is not available.";
        }

        await base.HandleError(problemDetails, cancellationToken);
      }
    }
  }
}
