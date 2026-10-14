#region Purpose
// What an IActionDisplayOverride shows for one command row: the full label and an optional description.
#endregion

#region Design
// Name replaces the whole row label (no "Owner: " prefix is added). A null Description keeps the
// catalog description or the generated one. Strings only, like CommandPaletteRow.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <param name="Name">Full row label, for example "Ask Akili".</param>
/// <param name="Description">Row description; null keeps the default.</param>
public sealed record ActionDisplay(string Name, string? Description = null);
