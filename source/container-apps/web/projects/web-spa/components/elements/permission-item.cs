#region Purpose
// One capability row: the caller's words and whether that capability is granted.
#endregion

#region Design
// Granted is a fact the page already decided. The grid only shows it. The label is
// the caller's text, rendered as text, not markup.
#endregion

namespace TimeWarp.Architecture.Components;

public sealed record PermissionItem(string Label, bool Granted);
