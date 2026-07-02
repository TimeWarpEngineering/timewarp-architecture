#region Purpose
// Cacheable-state base that guarantees every derived state has a valid, positive cache duration.
#endregion

#region Design
// TimeWarpCacheableState accepts any duration, so the guard lives here to fail fast at
// construction instead of producing a state that never (or always) reads as stale.
// The 30-second default lets derived states opt into caching without each one choosing a
// duration; states with real freshness requirements pass their own.
#endregion

namespace TimeWarp.Architecture.Features;

public abstract class BaseCacheableState<TState>:TimeWarpCacheableState<TState>
where TState : IState
{
  protected BaseCacheableState(TimeSpan? cacheDuration = null)
  {
    if (cacheDuration <= TimeSpan.Zero)
    {
      throw new ArgumentOutOfRangeException(nameof(cacheDuration), message: "Cache duration must be greater than zero.");
    }

    CacheDuration = cacheDuration ?? TimeSpan.FromSeconds(30);
  }
}
