#region Purpose
// Wire enum for the column the admin Principals list is ordered by.
#endregion

#region Design
// Closed set (task 328): the sortable columns of /Admin/Principals. CreatedAt is the store
// order (the default, ascending). DisplayName and Email come from the profile, so sorting by
// them needs admin.profiles.read. PascalCase names on the wire, like FeedbackListSortColumn.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

public enum PrincipalListSortColumn
{
  CreatedAt = 0,
  DisplayName = 1,
  Email = 2,
  Kind = 3,
}
