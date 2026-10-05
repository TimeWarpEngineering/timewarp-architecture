#region Purpose
// Contributes the hypermedia lab's contextual rows to Ctrl-K while the lab page is current.
#endregion

#region Design
// The ICommandPaletteContextSource for the lab: it answers only for HypermediaLabPage.Route and reads
// the store's current payloads through HypermediaLabRows, so palette rows follow the latest payload
// and vanish when the user leaves the page — no set/clear actions. Registered scoped in program.cs.
#endregion

namespace TimeWarp.Architecture.Features.HypermediaLab;

public sealed class HypermediaLabContextSource : ICommandPaletteContextSource
{
  private readonly IStore Store;
  private readonly IActionCatalog ActionCatalog;

  public HypermediaLabContextSource(IStore store, IActionCatalog actionCatalog)
  {
    Store = store;
    ActionCatalog = actionCatalog;
  }

  public IReadOnlyList<CommandPaletteRow> GetRows(string currentPath) =>
    string.Equals(currentPath, HypermediaLabPage.Route, StringComparison.OrdinalIgnoreCase)
      ? HypermediaLabRows.All(Store.GetState<HypermediaLabState>(), ActionCatalog)
      : [];
}
