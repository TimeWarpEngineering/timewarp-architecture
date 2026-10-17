#region Purpose
// Opens an attachment for its owner, or for an admin once it is filed.
#endregion

#region Design
// A missing id, another principal, and an admin asking for an unlinked draft are the same
// 404. After Link, the item owner may read. A cross-owner read is evaluated only under the
// scheme that authenticated this request (task 378 review M1). IAgentPermissionScopeSource
// returns the token scopes when the caller is that agent principal, and null otherwise.
// An agent-token caller is checked for feedback.read on the agent-token scheme only, so a
// human role assigned to the same principal cannot supply admin.feedback.read. A human
// session is checked for admin.feedback.read or feedback.read on the identity-session and
// mock-identity-session schemes only. admin.access alone opens the Admin area, not other
// people's files. An unlinked draft stays owner-only. The stream is the blob object; the
// endpoint writes it with the safe headers. A row whose bytes are gone (written before
// task 352 while bytes were in memory) is a 404 "attachment no longer available" for a
// caller who passed the access check; the row is left as it is.
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
    private readonly IAgentPermissionScopeSource AgentPermissionScopeSource;

    public Handler(
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IFeedbackAttachmentStore attachmentStore,
      IFeedbackAttachmentBlobStore blobStore,
      IPermissionEvaluator permissionEvaluator,
      IAgentPermissionScopeSource agentPermissionScopeSource)
    {
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      AttachmentStore = attachmentStore;
      BlobStore = blobStore;
      PermissionEvaluator = permissionEvaluator;
      AgentPermissionScopeSource = agentPermissionScopeSource;
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
      else if (!isOwner && !await HasCrossOwnerReadAsync(principalId.Value, cancellationToken).ConfigureAwait(false))
      {
        return FeedbackProblems.NotFound();
      }

      Stream? content = await BlobStore
        .OpenReadAsync(attachment.StorageKey, cancellationToken)
        .ConfigureAwait(false);
      if (content is null)
      {
        return FeedbackProblems.AttachmentGone();
      }

      return new Response(content, attachment.ContentType, attachment.FileName);
    }

    private async Task<bool> HasCrossOwnerReadAsync(PrincipalId principalId, CancellationToken cancellationToken)
    {
      if (AgentPermissionScopeSource.GetHeldScopesFor(principalId) is not null)
      {
        return await HasAsync(
          principalId,
          AuthenticationSchemeNames.AgentToken,
          PermissionIds.FeedbackRead,
          cancellationToken).ConfigureAwait(false);
      }

      return await HasOnHumanSchemeAsync(principalId, PermissionIds.AdminFeedbackRead, cancellationToken)
          .ConfigureAwait(false)
        || await HasOnHumanSchemeAsync(principalId, PermissionIds.FeedbackRead, cancellationToken)
          .ConfigureAwait(false);
    }

    private async Task<bool> HasOnHumanSchemeAsync(
      PrincipalId principalId,
      string permissionId,
      CancellationToken cancellationToken) =>
      await HasAsync(principalId, AuthenticationSchemeNames.IdentitySession, permissionId, cancellationToken)
        .ConfigureAwait(false)
      || await HasAsync(principalId, AuthenticationSchemeNames.MockIdentitySession, permissionId, cancellationToken)
        .ConfigureAwait(false);

    private Task<bool> HasAsync(
      PrincipalId principalId,
      string scheme,
      string permissionId,
      CancellationToken cancellationToken) =>
      PermissionEvaluator.HasPermissionAsync(principalId, scheme, permissionId, cancellationToken);
  }
}
