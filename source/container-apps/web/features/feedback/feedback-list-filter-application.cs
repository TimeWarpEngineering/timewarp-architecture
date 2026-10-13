#region Purpose
// Cross-owner query shape for the admin feedback list: filters, page window, and the result page.
#endregion

#region Design
// Statuses null means every status; an empty set matches nothing. FiledFrom is inclusive and
// FiledTo exclusive, so a day range is [day, day + 1). Skip/Take are the page window the
// handler computes from the contract's 1-based page. Stores order the whole match set by
// SortBy/SortDescending before taking the window (task 327), then by FiledAt descending when
// SortBy is another column, then by id ascending, so every page is stable. Kind and Status
// order by enum name, which is how EF stores them. The default is FiledAt descending (newest
// first). The sort column is the wire enum: it is a closed list of columns, not domain data.
// TotalCount counts every match, not just the page. Search (task 345) keeps items whose title
// contains the trimmed term, ignoring case; null or blank matches every title.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using TimeWarp.Architecture.Features.Feedback.Domain;

public sealed record FeedbackListFilter(
  IReadOnlyCollection<FeedbackStatus>? Statuses,
  FeedbackKind? Kind,
  DateTimeOffset? FiledFrom,
  DateTimeOffset? FiledTo,
  int Skip,
  int Take,
  FeedbackListSortColumn SortBy = FeedbackListSortColumn.FiledAt,
  bool SortDescending = true,
  string? Search = null);

public sealed record FeedbackPage(IReadOnlyList<FeedbackItem> Items, int TotalCount);
