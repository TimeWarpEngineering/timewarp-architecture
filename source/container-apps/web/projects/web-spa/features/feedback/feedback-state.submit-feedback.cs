#region Purpose
// SubmitFeedbackActionSet: files one item and keeps the id and permalink on the state.
#endregion

#region Design
// Visibility Both so the in-app assistant and WebMCP can file as the signed-in user. Submit is
// not on the read-only allow-list, so it stays approval-gated. The handler does not navigate;
// the receipt stays on the form. On success the handler puts the new item at the top of Items
// (built from the response; FiledAt is the client's clock, the list shows no time), so a human,
// in-app assistant, or WebMCP submit all refresh "Your filings" the same way and the page does not
// re-list. The agent payload is the API response (id and permalink included).
// includeDraftAttachments stays false unless the feedback form passes true, so another
// submit (for example a thumbs rating) does not file the user's pending uploads.
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
      public Action(
        FeedbackKind kind,
        string title,
        string body,
        bool emailCopy = false,
        bool includeDraftAttachments = false)
      {
        Kind = kind;
        Title = title;
        Body = body;
        EmailCopy = emailCopy;
        IncludeDraftAttachments = includeDraftAttachments;
      }

      public FeedbackKind Kind { get; }
      public string Title { get; }
      public string Body { get; }
      public bool EmailCopy { get; }
      public bool IncludeDraftAttachments { get; }
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
        List<Guid> attachmentIds = action.IncludeDraftAttachments
          ? FeedbackState.DraftAttachments.Select(draft => draft.AttachmentId).ToList()
          : [];
        return Task.FromResult<Command?>(new Command
        {
          Kind = action.Kind,
          Title = action.Title,
          Body = action.Body,
          EmailCopy = action.EmailCopy,
          AttachmentIds = attachmentIds,
        });
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        FeedbackState.LastReceipt = response;
        var linked = response.AttachmentIds.ToHashSet();
        FeedbackState.DraftAttachments = FeedbackState.DraftAttachments
          .Where(draft => !linked.Contains(draft.AttachmentId))
          .ToList();
        ListMyFeedback.Item filed = new(
          response.FeedbackItemId,
          response.Permalink,
          response.Kind,
          response.Title,
          DateTimeOffset.UtcNow);
        FeedbackState.Items =
        [
          filed,
          .. FeedbackState.Items.Where(item => item.FeedbackItemId != response.FeedbackItemId),
        ];
        AgentCallOutcome.Set(response);
        return Task.CompletedTask;
      }
    }
  }
}
