#region Purpose
// Opens any principal's feedback item for an administrator, with the full thread.
#endregion

#region Design
// No owner check: admin.feedback.read on the endpoint is the gate. A missing id is 404.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using TimeWarp.Architecture.Features.Feedback.Domain;
using static TimeWarp.Architecture.Features.Feedback.GetFeedbackForAdmin;

public sealed class GetFeedbackForAdmin
{
  public sealed class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IFeedbackStore FeedbackStore;
    private readonly IFeedbackAttachmentStore AttachmentStore;
    private readonly IProfileEmailLookup ProfileEmailLookup;
    private readonly FeedbackOwnerNames OwnerNames;

    public Handler(
      IFeedbackStore feedbackStore,
      IFeedbackAttachmentStore attachmentStore,
      IProfileEmailLookup profileEmailLookup,
      FeedbackOwnerNames ownerNames)
    {
      FeedbackStore = feedbackStore;
      AttachmentStore = attachmentStore;
      ProfileEmailLookup = profileEmailLookup;
      OwnerNames = ownerNames;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Query request,
      CancellationToken cancellationToken)
    {
      FeedbackItem? item = await FeedbackStore
        .FindAsync(FeedbackItemId.From(request.FeedbackItemId), cancellationToken)
        .ConfigureAwait(false);
      if (item is null)
      {
        return FeedbackProblems.NotFound();
      }

      return new Response(
        await FeedbackContractMap.AdminViewAsync(item, AttachmentStore, ProfileEmailLookup, OwnerNames, cancellationToken)
          .ConfigureAwait(false));
    }
  }
}
