#region Purpose
// Marks a page that opted in with [Page(Navigable = true)] and is therefore listed in the generated PageRegistry; implemented by the [Page] source generator.
#endregion

#region Design
// NavMenu, the Ctrl-K palette, and the agent's navigate and site_map tools all read
// NavigationDestinations over PageRegistry.All, so they cannot drift (task 300). This marker
// names the registry pages in the type system. Implement it only via [Page(Navigable = true)]; a hand-written implementation is caught by the
// web-spa PageRegistry test (every INavigationDestination type must be in PageRegistry.All).
#endregion

namespace TimeWarp.Architecture.Common.Interfaces;

[SuppressMessage
(
  "Design",
  "CA1040:Avoid empty interfaces",
  Justification = "Marker for PageRegistry membership; the registry test checks it both ways."
)]
public interface INavigationDestination;
