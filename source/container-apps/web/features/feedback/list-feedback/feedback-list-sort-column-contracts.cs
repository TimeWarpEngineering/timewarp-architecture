#region Purpose
// Wire enum for the column the admin feedback list is ordered by.
#endregion

#region Design
// Closed set (task 327): the sortable columns of /Admin/Feedback. Owner is absent because it
// is a raw principal id. PascalCase names on the wire, like FeedbackKind; the query string
// carries the name. FiledAt descending is the default (newest first).
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

public enum FeedbackListSortColumn
{
  FiledAt = 0,
  Title = 1,
  Kind = 2,
  Status = 3,
}
