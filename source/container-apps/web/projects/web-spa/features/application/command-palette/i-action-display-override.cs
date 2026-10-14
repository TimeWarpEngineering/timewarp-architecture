#region Purpose
// Runtime label override for a derived Ctrl-K command row, consulted per action when the palette opens.
#endregion

#region Design
// Task 301 derives command rows from ActionRoster with a compile-time convention label and optional
// [CatalogAction] metadata; some labels are only known at run time (task 361: "Ask {AssistantName}"
// from admin-set SiteSettings). Register implementations as scoped services; CommandPaletteRoster
// asks every one in registration order and the first non-null ActionDisplay wins. An override is
// display-only: it never changes the target, visibility, permissions or opt-out.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

public interface IActionDisplayOverride
{
  /// <summary>The display for <paramref name="action"/>, or null to keep the convention label.</summary>
  ActionDisplay? Find(ActionRosterEntry action);
}
