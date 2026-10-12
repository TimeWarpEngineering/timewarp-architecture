#region Purpose
// Lists the authenticated principal's feedback and reports whether an email copy can be offered.
#endregion

#region Design
// EmailCopyAvailable reads the existing profile email and does not create a profile. Another
// principal's items are never included. Kind and status map by name onto the contract enums.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using TimeWarp.Architecture.Features.Feedback.Domain;
using static TimeWarp.Architecture.Features.Feedback.ListMyFeedback;

public sealed class ListMyFeedback
{
  public sealed class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IFeedbackStore FeedbackStore;
    private readonly IProfileEmailLookup ProfileEmailLookup;

    public Handler(
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IFeedbackStore feedbackStore,
      IProfileEmailLookup profileEmailLookup)
    {
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      FeedbackStore = feedbackStore;
      ProfileEmailLookup = profileEmailLookup;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Query request,
      CancellationToken cancellationToken)
    {
      _ = request;
      PrincipalId? principalId = await CurrentPrincipalAccessor
        .GetCurrentPrincipalIdAsync(cancellationToken)
        .ConfigureAwait(false);
      if (principalId is null)
      {
        return FeedbackProblems.Unauthenticated();
      }

      IReadOnlyList<FeedbackItem> stored = await FeedbackStore
        .ListByOwnerAsync(principalId.Value.Value, cancellationToken)
        .ConfigureAwait(false);
      var items = new List<Item>(stored.Count);
      foreach (FeedbackItem item in stored)
      {
        items.Add(new Item(
          item.Id.Value,
          FeedbackPermalink.For(item.Id.Value),
          FeedbackContractMap.ToContract(item.Kind),
          item.Title,
          item.FiledAt,
          FeedbackContractMap.ToContract(item.Status)));
      }

      string? email = await ProfileEmailLookup
        .FindEmailAsync(principalId.Value.Value, cancellationToken)
        .ConfigureAwait(false);
      return new Response(items, emailCopyAvailable: !string.IsNullOrWhiteSpace(email));
    }
  }
}
