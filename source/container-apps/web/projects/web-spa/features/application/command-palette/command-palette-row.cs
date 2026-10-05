#region Purpose
// One Ctrl-K palette row: a page destination, a cataloged command, or a page-contributed contextual action, in a single shape.
#endregion

#region Design
// Rows hold strings only (Target is a URL or an ActionCatalogEntry.Name), never the
// ActionCatalogEntry or page Type: CommandPaletteState clones on dispatch, and a plain record
// clones without dragging executor delegates along. The runner resolves Target at run time.
// Contextual rows (task 275) are contributed by the current page through ICommandPaletteContextSource:
// Target is a catalog name, ArgumentsJson the JSON object of arguments the page's server payload
// bound, FollowUpTarget an optional parameterless catalog action run afterwards (refresh the payload
// that offered the row). Record equality over all fields is the "still offered" check — the runner
// only runs a contextual row that the current page still contributes, arguments included.
// RequiresInput marks a row the page can run only with user input (Rename's nickname): it stays in
// the contributed set so the page's button passes the same gate, but the palette — which has no
// argument UI — leaves it out of the roster.
// Contextual sorts first so a page's own actions head the empty-query list.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>What running a palette row does.</summary>
public enum CommandPaletteRowKind
{
  /// <summary>Execute a catalog action the current page offers, with the arguments it bound.</summary>
  Contextual,

  /// <summary>Navigate to a <see cref="PageRegistry"/> destination.</summary>
  Page,

  /// <summary>Execute a parameterless <c>[CatalogAction]</c>.</summary>
  Command
}

/// <param name="Name">Display name the user types against.</param>
/// <param name="Description">One line shown under the name; also searched.</param>
/// <param name="Kind">Navigate or execute.</param>
/// <param name="Target">Page URL for <see cref="CommandPaletteRowKind.Page"/>; catalog name for
/// <see cref="CommandPaletteRowKind.Command"/> and <see cref="CommandPaletteRowKind.Contextual"/>.</param>
/// <param name="ArgumentsJson">Contextual only: JSON object of catalog arguments keyed by parameter name.</param>
/// <param name="FollowUpTarget">Contextual only: parameterless catalog action run after <paramref name="Target"/>.</param>
/// <param name="RequiresInput">Contextual only: the page must supply input (a button with a field); the palette omits it.</param>
public sealed record CommandPaletteRow
(
  string Name,
  string Description,
  CommandPaletteRowKind Kind,
  string Target,
  string? ArgumentsJson = null,
  string? FollowUpTarget = null,
  bool RequiresInput = false
);
