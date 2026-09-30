#region Purpose
// Runs one Ctrl-K palette row: navigate to a page through RouteState, or execute a catalog command through the store.
#endregion

#region Design
// Called by the CommandPalette component after it closes the modal (pages sequence dispatches;
// handlers do not nest). Navigation goes through RouteState.ChangeRoute — the same state path
// NavMenu-driven pages use — so breadcrumbs and NotificationState's clear-on-navigation apply.
// Commands go through ActionCatalogEntry.Execute(store, [], ct): the generated, reflection-free
// dispatcher, which runs the action through the normal pipeline. Outcomes therefore land where
// that action already reports them — the shell NotificationState region (TWA0025) — and a
// command the catalog no longer knows is reported there too, never in a palette-local bar.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

public static class CommandPaletteRunner
{
  public static async Task RunAsync
  (
    CommandPaletteRow row,
    IStore store,
    IActionCatalog actionCatalog,
    CancellationToken cancellationToken
  )
  {
    switch (row.Kind)
    {
      case CommandPaletteRowKind.Page:
        await store.GetState<RouteState>().ChangeRoute(row.Target, externalCancellationToken: cancellationToken);
        break;

      case CommandPaletteRowKind.Command:
        ActionCatalogEntry? entry = actionCatalog.Find(row.Target);
        if (entry is null)
        {
          await store.GetState<NotificationState>().AddNotification
          (
            MessageBarIntent.Warning,
            $"{row.Name} is no longer available.",
            externalCancellationToken: cancellationToken
          );
          return;
        }

        await entry.Execute(store, [], cancellationToken);
        break;
    }
  }
}
