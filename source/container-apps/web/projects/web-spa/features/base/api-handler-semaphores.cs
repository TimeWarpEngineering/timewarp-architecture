#region Purpose
// Per-store, per-state-type semaphores shared by every ApiHandler closed generic type.
#endregion

#region Design
// IStore no longer exposes a per-state semaphore (TimeWarp.State 12.0.0-beta.11), so the SPA
// owns them. The table lives on this non-generic type: a static field on ApiHandler<,,> would be
// one table per closed generic, and two actions on the same state would never serialize.
// ConditionalWeakTable is keyed by the store so a circuit's semaphores die with its store.
#endregion

namespace TimeWarp.Architecture.Features;

internal static class ApiHandlerSemaphores
{
  private static readonly ConditionalWeakTable<IStore, ConcurrentDictionary<Type, SemaphoreSlim>> Semaphores = [];

  internal static SemaphoreSlim Get(IStore store, Type stateType) =>
    Semaphores
      .GetValue(store, static _ => [])
      .GetOrAdd(stateType, static _ => new SemaphoreSlim(1, 1));
}
