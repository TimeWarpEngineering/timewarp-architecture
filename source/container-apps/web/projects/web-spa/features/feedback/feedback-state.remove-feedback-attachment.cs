#region Purpose
// RemoveFeedbackAttachmentActionSet: drops one unfiled attachment from the draft list.
#endregion

#region Design
// Visibility Human. The response echoes the id. The row leaves DraftAttachments only when that
// id comes back, so a failed remove leaves the file on screen.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

using static RemoveFeedbackAttachment;

partial class FeedbackState
{
  public static class RemoveFeedbackAttachmentActionSet
  {
    [CatalogAction
    (
      Description = "Remove one unfiled feedback attachment.",
      Permissions = [PermissionIds.FeedbackFileSelf],
      Visibility = ActionVisibility.Human
    )]
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Action(Guid attachmentId)
      {
        AttachmentId = attachmentId;
      }

      public Guid AttachmentId { get; }
    }

    internal sealed class Handler : DefaultApiHandler<Action, Command, Response>
    {
      public Handler
      (
        IStore store,
        IWebServerApiService webServerApiService,
        ILogger<Handler> logger,
        IPublisher<ClientPipeline> publisher,
        IValidator<Command>? validator = null,
        AuthenticationStateProvider? authenticationStateProvider = null
      ) : base(store, webServerApiService, logger, publisher, validator, authenticationStateProvider)
      {
      }

      protected override Task<Command?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        return Task.FromResult<Command?>(new Command { AttachmentId = action.AttachmentId });
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        FeedbackState.DraftAttachments = FeedbackState.DraftAttachments
          .Where(draft => draft.AttachmentId != response.AttachmentId)
          .ToList();
        return Task.CompletedTask;
      }
    }
  }
}
