#region Purpose
// Opens an attachment for its owner, or for an admin once it is filed.
#endregion

#region Design
// A missing id, another principal, and an admin asking for an unlinked draft are the same
// 404. After Link, the item owner or a principal with admin.access on either human scheme
// may read. Agent-token is not a human scheme and is not consulted. The stream is the blob
// object; the endpoint writes it with the safe headers.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using TimeWarp.Architecture.Features.Feedback.Domain;
using static TimeWarp.Architecture.Features.Feedback.DownloadFeedbackAttachment;

public sealed class DownloadFeedbackAttachment
{
  public sealed class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IFeedbackAttachmentStore AttachmentStore;
    private readonly IFeedbackAttachmentBlobStore BlobStore;
    private readonly IPermissionEvaluator PermissionEvaluator;

    public Handler(
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IFeedbackAttachmentStore attachmentStore,
      IFeedbackAttachmentBlobStore blobStore,
      IPermissionEvaluator permissionEvaluator)
    {
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      AttachmentStore = attachmentStore;
      BlobStore = blobStore;
      PermissionEvaluator = permissionEvaluator;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Query request,
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
      if (attachment is null)
      {
        return FeedbackProblems.NotFound();
      }

      bool isOwner = attachment.OwnerPrincipalId == principalId.Value.Value;
      if (attachment.FeedbackItemId is null)
      {
        if (!isOwner)
        {
          return FeedbackProblems.NotFound();
        }
      }
      else if (!isOwner && !await IsAdminAsync(principalId.Value, cancellationToken).ConfigureAwait(false))
      {
        return FeedbackProblems.NotFound();
      }

      Stream? content = await BlobStore
        .OpenReadAsync(attachment.StorageKey, cancellationToken)
        .ConfigureAwait(false);
      if (content is null)
      {
        return FeedbackProblems.NotFound();
      }

      return new Response(content, attachment.ContentType, attachment.FileName);
    }

    private async Task<bool> IsAdminAsync(PrincipalId principalId, CancellationToken cancellationToken)
    {
      if (await PermissionEvaluator.HasPermissionAsync(
        principalId,
        AuthenticationSchemeNames.IdentitySession,
        PermissionIds.AdminAccess,
        cancellationToken).ConfigureAwait(false))
      {
        return true;
      }

      return await PermissionEvaluator.HasPermissionAsync(
        principalId,
        AuthenticationSchemeNames.MockIdentitySession,
        PermissionIds.AdminAccess,
        cancellationToken).ConfigureAwait(false);
    }
  }
}
