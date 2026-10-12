#region Purpose
// Returns one feedback item to its owner.
#endregion

#region Design
// A missing id and an id owned by another principal are both 404. The response kind is mapped
// back by name onto the contract enum. Attachments are the rows linked to this item. Status and
// the thread are the filer's view (FeedbackContractMap.FilerThread: no internal notes).
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using TimeWarp.Architecture.Features.Feedback.Domain;
using static TimeWarp.Architecture.Features.Feedback.GetFeedback;

public sealed class GetFeedback
{
  public sealed class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IFeedbackStore FeedbackStore;
    private readonly IFeedbackAttachmentStore AttachmentStore;

    public Handler(
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IFeedbackStore feedbackStore,
      IFeedbackAttachmentStore attachmentStore)
    {
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      FeedbackStore = feedbackStore;
      AttachmentStore = attachmentStore;
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

      FeedbackItem? item = await FeedbackStore
        .FindAsync(FeedbackItemId.From(request.FeedbackItemId), cancellationToken)
        .ConfigureAwait(false);
      if (item is null || item.OwnerPrincipalId != principalId.Value.Value)
      {
        return FeedbackProblems.NotFound();
      }

      List<Attachment> attachments = await FeedbackContractMap
        .AttachmentsAsync(AttachmentStore, item.Id, cancellationToken)
        .ConfigureAwait(false);

      return new Response(
        item.Id.Value,
        FeedbackPermalink.For(item.Id.Value),
        FeedbackContractMap.ToContract(item.Kind),
        item.Title,
        item.Body,
        item.FiledAt,
        attachments,
        FeedbackContractMap.ToContract(item.Status),
        FeedbackContractMap.FilerThread(item));
    }
  }
}
