#region Purpose
// One labelled group of capability rows for PermissionGrid.
#endregion

#region Design
// The page builds the groups. The grid does not decide who is granted what, and it
// does not look up a role. A blank label is omitted. Items may be null; the grid
// treats that as no rows.
#endregion

namespace TimeWarp.Architecture.Components;

public sealed record PermissionGroup(string Label, IReadOnlyList<PermissionItem>? Items);
