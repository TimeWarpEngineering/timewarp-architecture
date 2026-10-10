#region Purpose
// Stores one unlinked attachment for the authenticated principal.
#endregion

#region Design
// The owner comes from ICurrentPrincipalAccessor. The stream is read once, under the size
// cap, and checked against the claimed type before either store is touched. A principal
// may hold at most FeedbackAttachment.MaxPerItem unlinked files so a draft cannot grow
// without bound. The blob is written first; if the row insert throws, the blob is deleted.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using TimeWarp.Architecture.Features.Feedback.Domain;
using static TimeWarp.Architecture.Features.Feedback.UploadFeedbackAttachment;
using DownloadFeedbackAttachmentContract = TimeWarp.Architecture.Features.Feedback.DownloadFeedbackAttachment;

public sealed class UploadFeedbackAttachment
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

      string? fileName = FeedbackAttachmentNames.Normalize(request.FileName);
      if (fileName is null)
      {
        return FeedbackProblems.InvalidFileName();
      }

      string? contentType = FeedbackAttachmentRules.CanonicalContentType(request.ContentType);
      if (contentType is null)
      {
        return FeedbackProblems.UnsupportedMedia();
      }

      OneOf<byte[], SharedProblemDetails> body = await FeedbackAttachmentContent
        .ReadAsync(request.Content, contentType, cancellationToken)
        .ConfigureAwait(false);
      if (body.IsT1)
      {
        return body.AsT1;
      }

      byte[] bytes = body.AsT0;
      Guid ownerId = principalId.Value.Value;
      int pending = await AttachmentStore
        .CountUnlinkedByOwnerAsync(ownerId, cancellationToken)
        .ConfigureAwait(false);
      if (pending >= FeedbackAttachment.MaxPerItem)
      {
        return FeedbackProblems.TooManyAttachments();
      }

      var attachment = FeedbackAttachment.Create(
        ownerId,
        fileName,
        contentType,
        bytes.LongLength,
        DateTimeOffset.UtcNow);

      await BlobStore
        .PutAsync(attachment.StorageKey, bytes, contentType, cancellationToken)
        .ConfigureAwait(false);
      try
      {
        await AttachmentStore.AddAsync(attachment, cancellationToken).ConfigureAwait(false);
      }
      catch
      {
        await BlobStore.DeleteAsync(attachment.StorageKey, CancellationToken.None).ConfigureAwait(false);
        throw;
      }

      return new Response(
        attachment.Id.Value,
        attachment.FileName,
        attachment.ContentType,
        attachment.Size,
        DownloadFeedbackAttachmentContract.DownloadPath(attachment.Id.Value));
    }
  }
}
