#region Purpose
// Lists the authenticated principal's feedback and reports whether an email copy can be offered.
#endregion

#region Design
// EmailCopyAvailable reads the existing profile email and does not create a profile. Another
// principal's items are never included. Kind and status map by name onto the contract enums.
// Search (task 345) keeps titles containing the trimmed term, ignoring case; blank keeps all.
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

    public async ValueTask<OneOf<Response, SharedProblemDetails>> Handle(
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

      IReadOnlyList<FeedbackItem> stored = await FeedbackStore
        .ListByOwnerAsync(principalId.Value.Value, cancellationToken)
        .ConfigureAwait(false);
      var items = new List<Item>(stored.Count);
      string? search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
      foreach (FeedbackItem item in stored)
      {
        if (search is not null && !item.Title.Contains(search, StringComparison.OrdinalIgnoreCase))
        {
          continue;
        }

        items.Add(new Item(
          feedbackItemId: item.Id.Value,
          permalink: FeedbackPermalink.For(item.Id.Value),
          kind: FeedbackContractMap.ToContract(item.Kind),
          title: item.Title,
          filedAt: item.FiledAt,
          status: FeedbackContractMap.ToContract(item.Status)));
      }

      string? email = await ProfileEmailLookup
        .FindEmailAsync(principalId.Value.Value, cancellationToken)
        .ConfigureAwait(false);
      return new Response(items: items, emailCopyAvailable: !string.IsNullOrWhiteSpace(email));
    }
  }
}
