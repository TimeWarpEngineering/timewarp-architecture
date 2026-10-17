#region Purpose
// Sets a feedback item's review severity and records it in the thread. Sends no email.
#endregion

#region Design
// The author is the authenticated caller (ICurrentPrincipalAccessor), never the client.
// Unset and undefined severities are 400; the current severity is 409 (the aggregate refuses
// both, and the handler answers first so the client gets a problem, not an exception). Status
// is left alone. There is no mail sender: a review is not a status call. A concurrent writer
// is 409 Changed.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using TimeWarp.Architecture.Features.Feedback.Domain;
using static TimeWarp.Architecture.Features.Feedback.SetFeedbackSeverity;
using DomainSeverity = TimeWarp.Architecture.Features.Feedback.Domain.FeedbackSeverity;

public sealed class SetFeedbackSeverity
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

      if (FeedbackContractMap.ToDomain(request.Severity) is not { } severity || severity == DomainSeverity.Unset)
      {
        return FeedbackProblems.InvalidSeverity();
      }

      FeedbackItem? item = await FeedbackStore
        .FindAsync(FeedbackItemId.From(request.FeedbackItemId), cancellationToken)
        .ConfigureAwait(false);
      if (item is null)
      {
        return FeedbackProblems.NotFound();
      }

      if (!item.CanSetSeverityTo(severity))
      {
        return FeedbackProblems.SeverityUnchanged(severity.ToString());
      }

      item.SetSeverity(severity, principalId.Value.Value, DateTimeOffset.UtcNow);
      if (!await FeedbackStore.UpdateAsync(item, cancellationToken).ConfigureAwait(false))
      {
        return FeedbackProblems.Changed();
      }

      return new Response(
        await FeedbackContractMap
          .AdminViewAsync(item, AttachmentStore, ProfileEmailLookup, OwnerNames, cancellationToken)
          .ConfigureAwait(false));
    }
  }
}
