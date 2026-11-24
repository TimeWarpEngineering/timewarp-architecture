#region Purpose
// One parent row and its child item ids for PermissionTree.
#endregion

#region Design
// The page builds the groups. Key names the group in data-qa. Label is the parent row's
// text, rendered as text, not markup. Items are the selectable ids. Each id is also its
// row label and its data-qa suffix. A null Items list is no rows. The tree does not decide
// which ids are granted or locked.
#endregion

namespace TimeWarp.Architecture.Components;

public sealed record PermissionTreeGroup(string Key, string Label, IReadOnlyList<string>? Items);
