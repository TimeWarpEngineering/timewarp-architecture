#region Purpose
// Runs one Ctrl-K palette row: navigate to a page through RouteState, execute a catalog command, or run a page's contextual action.
#endregion

#region Design
// Called by the CommandPalette component after it closes the modal (pages sequence dispatches;
// handlers do not nest). Navigation goes through RouteState.ChangeRoute — the same state path
// NavMenu-driven pages use — so breadcrumbs and NotificationState's clear-on-navigation apply.
// Commands go through ActionCatalogEntry.Execute(store, [], ct): the generated, reflection-free
// dispatcher, which runs the action through the normal pipeline. Outcomes therefore land where
// that action already reports them — the shell NotificationState region (TWA0025) — and a
// command the catalog no longer knows is reported there too, never in a palette-local bar.
// Contextual rows (task 275) run through RunContextualAsync, which the lab page's buttons call too,
// so a button and its palette row share one fail-closed path: the row must still be contributed by
// the current page (CommandPaletteContext.IsOffered — record equality, arguments included), its
// Target must be a catalog entry, and its arguments plus the caller's input must bind
// (ContextualActionArguments). Input only fills parameters the row left unbound; it can never
// override an argument the server bound. Every refusal is a Warning in the shell region. After the
// action, the row's FollowUpTarget (a parameterless catalog action) runs — that is how the page's
// payload refreshes, because this helper, not a handler, sequences the two dispatches (handlers do
// not dispatch, TWS0002). Catalog Permissions/Visibility are not consulted for contextual rows: the
// server already decided the offer for this caller and enforces it again on the real endpoint.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using System.Text.Json;

public static class CommandPaletteRunner
{
  public static async Task RunAsync
  (
    CommandPaletteRow row,
    IStore store,
    IActionCatalog actionCatalog,
    CancellationToken cancellationToken,
    CommandPaletteContext? context = null
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
          await WarnAsync(store, $"{row.Name} is no longer available.", cancellationToken);
          return;
        }

        await entry.Execute(store, [], cancellationToken);
        break;

      case CommandPaletteRowKind.Contextual:
        await RunContextualAsync(row, input: null, store, actionCatalog, context, cancellationToken);
        break;
    }
  }

  /// <summary>
  /// Runs a contextual row the current page still offers, with <paramref name="input"/> filling the
  /// parameters the offer left unbound; fails closed with a shell warning otherwise.
  /// </summary>
  public static async Task RunContextualAsync
  (
    CommandPaletteRow row,
    IReadOnlyDictionary<string, JsonElement>? input,
    IStore store,
    IActionCatalog actionCatalog,
    CommandPaletteContext? context,
    CancellationToken cancellationToken
  )
  {
    if (context?.IsOffered(row) != true)
    {
      await WarnAsync(store, $"{row.Name} is not offered here now.", cancellationToken);
      return;
    }

    ActionCatalogEntry? entry = actionCatalog.Find(row.Target);
    if (entry is null)
    {
      await WarnAsync(store, $"{row.Name}: '{row.Target}' is not an action this app knows.", cancellationToken);
      return;
    }

    OneOf<object?[], string> bound = Bind(entry, row.ArgumentsJson, input);
    if (bound.TryPickT1(out string? reason, out object?[]? arguments))
    {
      await WarnAsync(store, $"{row.Name} was refused: {reason}.", cancellationToken);
      return;
    }

    await entry.Execute(store, arguments, cancellationToken);

    if (row.FollowUpTarget is { } followUpTarget)
    {
      ActionCatalogEntry? followUp = actionCatalog.Find(followUpTarget);
      if (followUp is null)
      {
        await WarnAsync(store, $"{row.Name}: the refresh action '{followUpTarget}' is not available.", cancellationToken);
        return;
      }

      await followUp.Execute(store, [], cancellationToken);
    }
  }

  private static OneOf<object?[], string> Bind
  (
    ActionCatalogEntry entry,
    string? argumentsJson,
    IReadOnlyDictionary<string, JsonElement>? input
  )
  {
    OneOf<Dictionary<string, JsonElement>, string> parsed = ContextualActionArguments.Parse(argumentsJson);
    if (parsed.TryPickT1(out string? reason, out Dictionary<string, JsonElement>? arguments))
    {
      return reason;
    }

    foreach ((string name, JsonElement value) in input ?? new Dictionary<string, JsonElement>())
    {
      if (!arguments.TryAdd(name, value))
      {
        return $"input cannot replace the offered '{name}'";
      }
    }

    return ContextualActionArguments.Bind(entry, arguments);
  }

  private static Task WarnAsync(IStore store, string title, CancellationToken cancellationToken) =>
    store.GetState<NotificationState>().AddNotification
    (
      MessageBarIntent.Warning,
      title,
      externalCancellationToken: cancellationToken
    );
}
