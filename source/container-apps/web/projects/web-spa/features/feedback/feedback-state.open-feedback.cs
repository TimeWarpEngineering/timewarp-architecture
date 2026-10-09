#region Purpose
// OpenFeedbackActionSet: loads one filing by id and opens its permalink.
#endregion

#region Design
// Visibility Both and on the read-only allow-list. The handler navigates only when the browser
// is not already on that permalink, so the detail page can dispatch it without a loop.
// A 404 (missing or another user's item) clears Current and sets a non-revealing LoadError,
// then still publishes the problem toast.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

using static GetFeedback;

partial class FeedbackState
{
  public static class OpenFeedbackActionSet
  {
    [CatalogAction
    (
      Description = "Open one of your feedback filings by id.",
      Permissions = [PermissionIds.FeedbackFileSelf],
      Visibility = ActionVisibility.Both
    )]
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Action(Guid feedbackItemId)
      {
        FeedbackItemId = feedbackItemId;
      }

      public Guid FeedbackItemId { get; }
    }

    internal sealed class Handler : DefaultApiHandler<Action, Query, Response>
    {
      private readonly AgentCallOutcome AgentCallOutcome;
      private readonly NavigationManager NavigationManager;

      public Handler
      (
        IStore store,
        IWebServerApiService webServerApiService,
        ILogger<Handler> logger,
        IPublisher<ClientPipeline> publisher,
        AgentCallOutcome agentCallOutcome,
        NavigationManager navigationManager,
        IValidator<Query>? validator = null,
        AuthenticationStateProvider? authenticationStateProvider = null
      ) : base(store, webServerApiService, logger, publisher, validator, authenticationStateProvider)
      {
        AgentCallOutcome = agentCallOutcome;
        NavigationManager = navigationManager;
      }

      protected override Task<Query?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        return Task.FromResult<Query?>(new Query { FeedbackItemId = action.FeedbackItemId });
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        FeedbackState.Current = response;
        FeedbackState.LoadError = null;
        AgentCallOutcome.Set(response);
        string current = PageAgentScope.Normalize(NavigationManager.ToBaseRelativePath(NavigationManager.Uri));
        string target = PageAgentScope.Normalize(response.Permalink);
        if (!string.Equals(current, target, StringComparison.OrdinalIgnoreCase))
        {
          NavigationManager.NavigateTo(response.Permalink);
        }

        return Task.CompletedTask;
      }

      protected override async Task HandleError(SharedProblemDetails problemDetails, CancellationToken cancellationToken)
      {
        if (problemDetails.Status == 404)
        {
          FeedbackState.Current = null;
          FeedbackState.LoadError = "This feedback item is not available.";
        }

        await base.HandleError(problemDetails, cancellationToken);
      }
    }
  }
}
