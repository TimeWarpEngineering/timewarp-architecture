#region Purpose
// What the shell app-bar search slot renders.
#endregion

#region Design
// Palette is the default: the read-only field that opens the command palette.
// Hidden removes that field. Custom renders the page's Search fragment in the
// same slot. Ctrl-K stays registered in every mode; only the visible field changes.
// The shell does not interpret custom search text.
#endregion

namespace TimeWarp.Architecture;

/// <summary>App-bar search slot on <c>TimeWarpPage</c>.</summary>
public enum ShellSearchMode
{
  /// <summary>Read-only field that opens the command palette.</summary>
  Palette = 0,

  /// <summary>No search field.</summary>
  Hidden = 1,

  /// <summary>The page's Search fragment.</summary>
  Custom = 2,
}
