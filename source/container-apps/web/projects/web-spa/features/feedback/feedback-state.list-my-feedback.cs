#region Purpose
// ListMyFeedbackActionSet: loads the filer's items and whether an email copy can be offered.
#endregion

#region Design
// Parameterless and Visibility Both, so it is a Ctrl-K command and a read-only agent tool.
// It does not clear LastReceipt. Success sets FilingsLoaded, including when the filer has no
// rows yet. From any page other than /Feedback it navigates to the list so the palette command
// shows the filings. A detail route (/Feedback/{id}) also navigates to the list; that is the
// command's job.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

using static ListMyFeedback;

partial class FeedbackState
{
  public static class ListMyFeedbackActionSet
  {
    [CatalogAction
    (
      Description = "List the feedback you have filed.",
      Permissions = [PermissionIds.FeedbackFileSelf],
      Visibility = ActionVisibility.Both
    )]
    [TrackAction]
    public sealed class Action : IBaseAction;

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
        _ = action;
        return Task.FromResult<Query?>(new Query());
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        FeedbackState.Items = response.Items;
        FeedbackState.FilingsLoaded = true;
        FeedbackState.EmailCopyAvailable = response.EmailCopyAvailable;
        AgentCallOutcome.Set(response);
        string current = PageAgentScope.Normalize(NavigationManager.ToBaseRelativePath(NavigationManager.Uri));
        if (!string.Equals(current, "/Feedback", StringComparison.OrdinalIgnoreCase))
        {
          NavigationManager.NavigateTo("/Feedback");
        }

        return Task.CompletedTask;
      }
    }
  }
}
