#region Purpose
// Adds an admin-only internal note to a feedback item.
#endregion

#region Design
// The author is the authenticated administrator. No mail: the filer never learns a note
// exists. A concurrent writer is 409 Changed.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using TimeWarp.Architecture.Features.Feedback.Domain;
using static TimeWarp.Architecture.Features.Feedback.AddFeedbackNote;

public sealed class AddFeedbackNote
{
  public sealed class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IFeedbackStore FeedbackStore;
    private readonly IFeedbackAttachmentStore AttachmentStore;
    private readonly IProfileEmailLookup ProfileEmailLookup;
    private readonly FeedbackOwnerNames OwnerNames;

    public Handler(
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IFeedbackStore feedbackStore,
      IFeedbackAttachmentStore attachmentStore,
      IProfileEmailLookup profileEmailLookup,
      FeedbackOwnerNames ownerNames)
    {
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      FeedbackStore = feedbackStore;
      AttachmentStore = attachmentStore;
      ProfileEmailLookup = profileEmailLookup;
      OwnerNames = ownerNames;
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

      if (!FeedbackProblems.IsValidMessage(request.Body))
      {
        return FeedbackProblems.InvalidMessage();
      }

      FeedbackItem? item = await FeedbackStore
        .FindAsync(FeedbackItemId.From(request.FeedbackItemId), cancellationToken)
        .ConfigureAwait(false);
      if (item is null)
      {
        return FeedbackProblems.NotFound();
      }

      item.AddInternalNote(principalId.Value.Value, request.Body, DateTimeOffset.UtcNow);
      if (!await FeedbackStore.UpdateAsync(item, cancellationToken).ConfigureAwait(false))
      {
        return FeedbackProblems.Changed();
      }

      return new Response(
        await FeedbackContractMap.AdminViewAsync(item, AttachmentStore, ProfileEmailLookup, OwnerNames, cancellationToken)
          .ConfigureAwait(false));
    }
  }
}
