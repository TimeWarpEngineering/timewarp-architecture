#region Purpose
// One Ctrl-K palette row: a page destination or a cataloged command, in a single shape.
#endregion

#region Design
// Rows hold strings only (Target is a URL or an ActionCatalogEntry.Name), never the
// ActionCatalogEntry or page Type: CommandPaletteState clones on dispatch, and a plain record
// clones without dragging executor delegates along. The runner resolves Target at run time.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>What running a palette row does.</summary>
public enum CommandPaletteRowKind
{
  /// <summary>Navigate to a <see cref="PageRegistry"/> destination.</summary>
  Page,

  /// <summary>Execute a parameterless <c>[CatalogAction]</c>.</summary>
  Command
}

/// <param name="Name">Display name the user types against.</param>
/// <param name="Description">One line shown under the name; also searched.</param>
/// <param name="Kind">Navigate or execute.</param>
/// <param name="Target">Page URL for <see cref="CommandPaletteRowKind.Page"/>; catalog name for
/// <see cref="CommandPaletteRowKind.Command"/>.</param>
public sealed record CommandPaletteRow(string Name, string Description, CommandPaletteRowKind Kind, string Target);
