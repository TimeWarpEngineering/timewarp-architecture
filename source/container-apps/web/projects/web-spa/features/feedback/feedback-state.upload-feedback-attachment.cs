#region Purpose
// UploadFeedbackAttachmentActionSet: stores one file and keeps it on the draft list.
#endregion

#region Design
// Visibility Human: the bytes are a browser concern, not an agent argument. The handler sends
// the same raw-body command the paste hook and the file picker share. HandleSuccess appends
// the server id. A preview image stays in the page, not on this state.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

using static UploadFeedbackAttachment;

partial class FeedbackState
{
  public static class UploadFeedbackAttachmentActionSet
  {
    [CatalogAction
    (
      Description = "Upload one feedback attachment before filing.",
      Permissions = [PermissionIds.FeedbackFileSelf],
      Visibility = ActionVisibility.Human
    )]
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Action(string fileName, string contentType, byte[] content)
      {
        FileName = fileName;
        ContentType = contentType;
        Content = content;
      }

      public string FileName { get; }
      public string ContentType { get; }
      public byte[] Content { get; }
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
        return Task.FromResult<Command?>(new Command
        {
          FileName = action.FileName,
          ContentType = action.ContentType,
          Content = new MemoryStream(action.Content),
        });
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        FeedbackState.DraftAttachments =
        [
          .. FeedbackState.DraftAttachments,
          new DraftAttachment(response.AttachmentId, response.FileName, response.ContentType),
        ];
        return Task.CompletedTask;
      }
    }
  }
}
