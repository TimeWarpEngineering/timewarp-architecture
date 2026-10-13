#region Purpose
// Wire enum for the column the admin feedback list is ordered by.
#endregion

#region Design
// Closed set (task 327): the sortable columns of /Admin/Feedback. Owner is absent: the display
// name (task 336) lives in the profile slice and the store pages in the database, so an owner
// sort would load every match; if added, it must 403 without admin.profiles.read (task 328). PascalCase names on the wire, like FeedbackKind; the query string
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
