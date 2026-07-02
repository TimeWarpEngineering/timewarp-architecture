#region Purpose
// Derives a page's EditMode from the route convention /feature/{New|Edit|View}/...
#endregion

#region Design
// Encoding the mode in the URL (instead of component state) keeps it correct across deep links,
// refresh, and back navigation, and lets one page component serve all three modes.
// Unmatched routes default to View — read-only is the safe fallback.
// GeneratedRegex avoids runtime regex compilation cost on the WASM client.
#endregion

namespace TimeWarp.Architecture.Services;

public static partial class RouteModeResolver
{
  [GeneratedRegex("/.*/(New|Edit|View)(/|$)")]
  private static partial Regex EditModeRegex();

  public static EditMode GetEditMode(string route)
  {
    EditMode result = EditMode.View;
    Match match = EditModeRegex().Match(route);

    if (match.Success)
    {
      string action = match.Groups[1].Value;
      if (Enum.TryParse(action, ignoreCase: true, out EditMode editMode))
      {
        result = editMode;
      }
    }

    return result;
  }
}
