#region Purpose
// SubmitFeedbackActionSet: files one item and keeps the id and permalink on the state.
#endregion

#region Design
// Visibility Both so the in-app assistant and WebMCP can file as the signed-in user. Submit is
// not on the read-only allow-list, so it stays approval-gated. The handler does not navigate;
// the receipt stays on the form. The page prepends nothing itself — it re-lists after success.
// The agent payload is the API response (id and permalink included).
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

using static SubmitFeedback;

partial class FeedbackState
{
  public static class SubmitFeedbackActionSet
  {
    [CatalogAction
    (
      Description = "File feedback (bug report, feature request, complaint, or other) and return its id and permalink.",
      Permissions = [PermissionIds.FeedbackFileSelf],
      Visibility = ActionVisibility.Both
    )]
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Action(FeedbackKind kind, string title, string body, bool emailCopy = false)
      {
        Kind = kind;
        Title = title;
        Body = body;
        EmailCopy = emailCopy;
      }

      public FeedbackKind Kind { get; }
      public string Title { get; }
      public string Body { get; }
      public bool EmailCopy { get; }
    }

    internal sealed class Handler : DefaultApiHandler<Action, Command, Response>
    {
      private readonly AgentCallOutcome AgentCallOutcome;

      public Handler
      (
        IStore store,
        IWebServerApiService webServerApiService,
        ILogger<Handler> logger,
        IPublisher<ClientPipeline> publisher,
        AgentCallOutcome agentCallOutcome,
        IValidator<Command>? validator = null,
        AuthenticationStateProvider? authenticationStateProvider = null
      ) : base(store, webServerApiService, logger, publisher, validator, authenticationStateProvider)
      {
        AgentCallOutcome = agentCallOutcome;
      }

      protected override Task<Command?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        return Task.FromResult<Command?>(new Command
        {
          Kind = action.Kind,
          Title = action.Title,
          Body = action.Body,
          EmailCopy = action.EmailCopy,
        });
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        FeedbackState.LastReceipt = response;
        AgentCallOutcome.Set(response);
        return Task.CompletedTask;
      }
    }
  }
}
