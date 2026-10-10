#region Purpose
// Deletes an unlinked attachment owned by the caller, row and blob.
#endregion

#region Design
// Filed attachments are part of the item, so Link makes remove a 409. Another principal
// and a missing id are the same 404. The row is removed first; the blob delete follows
// so a failed blob delete cannot leave a row pointing at nothing the caller can still see.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using TimeWarp.Architecture.Features.Feedback.Domain;
using static TimeWarp.Architecture.Features.Feedback.RemoveFeedbackAttachment;

public sealed class RemoveFeedbackAttachment
{
  public sealed class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IFeedbackAttachmentStore AttachmentStore;
    private readonly IFeedbackAttachmentBlobStore BlobStore;

    public Handler(
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IFeedbackAttachmentStore attachmentStore,
      IFeedbackAttachmentBlobStore blobStore)
    {
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      AttachmentStore = attachmentStore;
      BlobStore = blobStore;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Command request,
      CancellationToken cancellationToken)
    {
      PrincipalId? principalId = await CurrentPrincipalAccessor
        .GetCurrentPrincipalIdAsync(cancellationToken)
        .ConfigureAwait(false);
      if (principalId is null)
      {
        return FeedbackProblems.Unauthenticated();
      }

      FeedbackAttachment? attachment = await AttachmentStore
        .FindAsync(FeedbackAttachmentId.From(request.AttachmentId), cancellationToken)
        .ConfigureAwait(false);
      if (attachment is null || attachment.OwnerPrincipalId != principalId.Value.Value)
      {
        return FeedbackProblems.NotFound();
      }

      if (attachment.FeedbackItemId is not null)
      {
        return FeedbackProblems.AlreadyFiled();
      }

      string storageKey = attachment.StorageKey;
      await AttachmentStore.RemoveAsync(attachment.Id, cancellationToken).ConfigureAwait(false);
      await BlobStore.DeleteAsync(storageKey, cancellationToken).ConfigureAwait(false);
      return new Response(attachment.Id.Value);
    }
  }
}
