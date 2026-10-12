#region Purpose
// Cross-owner query shape for the admin feedback list: filters, page window, and the result page.
#endregion

#region Design
// Statuses null means every status; an empty set matches nothing. FiledFrom is inclusive and
// FiledTo exclusive, so a day range is [day, day + 1). Skip/Take are the page window the
// handler computes from the contract's 1-based page; stores order by FiledAt descending, then
// id, so pages are stable. TotalCount counts every match, not just the page.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using TimeWarp.Architecture.Features.Feedback.Domain;

public sealed record FeedbackListFilter(
  IReadOnlyCollection<FeedbackStatus>? Statuses,
  FeedbackKind? Kind,
  DateTimeOffset? FiledFrom,
  DateTimeOffset? FiledTo,
  int Skip,
  int Take);

public sealed record FeedbackPage(IReadOnlyList<FeedbackItem> Items, int TotalCount);
