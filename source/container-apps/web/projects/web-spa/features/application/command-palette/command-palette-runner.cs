#region Purpose
// Runs one Ctrl-K palette row: navigate to a page through RouteState, or execute a derived command through the store.
#endregion

#region Design
// Called by the CommandPalette component after it closes the modal (pages sequence dispatches;
// handlers do not nest). Navigation goes through RouteState.ChangeRoute — the same state path
// NavMenu-driven pages use — so breadcrumbs and NotificationState's clear-on-navigation apply.
// Commands go through ActionRosterEntry.Execute(store, ct) (task 301): the generated, reflection-free
// call of the action's State.Method, which runs the action through the normal pipeline. Outcomes
// therefore land where that action already reports them — the shell NotificationState region
// (TWA0025) — and a command the roster no longer knows (or can no longer run without arguments) is
// reported there too, never in a palette-local bar.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

public static class CommandPaletteRunner
{
  public static async Task RunAsync
  (
    CommandPaletteRow row,
    IStore store,
    IEnumerable<ActionRosterEntry> actions,
    CancellationToken cancellationToken
  )
  {
    switch (row.Kind)
    {
      case CommandPaletteRowKind.Page:
        await store.GetState<RouteState>().ChangeRoute(row.Target, externalCancellationToken: cancellationToken);
        break;

      case CommandPaletteRowKind.Command:
        ActionRosterEntry? entry = actions.FirstOrDefault(action => action.Name == row.Target);
        if (entry?.Execute is null)
        {
          await store.GetState<NotificationState>().AddNotification
          (
            MessageBarIntent.Warning,
            $"{row.Name} is no longer available.",
            externalCancellationToken: cancellationToken
          );
          return;
        }

        await entry.Execute(store, cancellationToken);
        break;
      default:
        break;
    }
  }
}
