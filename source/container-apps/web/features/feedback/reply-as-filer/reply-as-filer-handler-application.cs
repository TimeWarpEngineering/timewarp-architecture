#region Purpose
// Adds the filer's answer to the thread of one of their own feedback items.
#endregion

#region Design
// Owner comes from ICurrentPrincipalAccessor. Another principal's item and a missing id are the
// same 404, matching GetFeedback. No mail: administrators read the thread in the admin view.
// The response is the filer-visible thread (FeedbackContractMap.FilerThread).
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using TimeWarp.Architecture.Features.Feedback.Domain;
using static TimeWarp.Architecture.Features.Feedback.ReplyAsFiler;

public sealed class ReplyAsFiler
{
  public sealed class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IFeedbackStore FeedbackStore;

    public Handler(ICurrentPrincipalAccessor currentPrincipalAccessor, IFeedbackStore feedbackStore)
    {
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      FeedbackStore = feedbackStore;
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
      if (item is null || item.OwnerPrincipalId != principalId.Value.Value)
      {
        return FeedbackProblems.NotFound();
      }

      item.AddFilerReply(principalId.Value.Value, request.Body, DateTimeOffset.UtcNow);
      if (!await FeedbackStore.UpdateAsync(item, cancellationToken).ConfigureAwait(false))
      {
        return FeedbackProblems.Changed();
      }

      return new Response(
        item.Id.Value,
        FeedbackContractMap.ToContract(item.Status),
        FeedbackContractMap.FilerThread(item));
    }
  }
}
