#region Purpose
// Marks a page that opted in with [Page(Navigable = true)] and is therefore listed in the generated PageRegistry; implemented by the [Page] source generator.
#endregion

#region Design
// TimeWarpNavLink constrains TPage to this marker, so every NavMenu link is a registry entry at
// compile time — the menu and the Ctrl-K palette read the same destinations and cannot drift.
#endregion

namespace TimeWarp.Architecture.Common.Interfaces;

[SuppressMessage
(
  "Design",
  "CA1040:Avoid empty interfaces",
  Justification = "Compile-time marker: TimeWarpNavLink's generic constraint enforces registry membership."
)]
public interface INavigationDestination;
