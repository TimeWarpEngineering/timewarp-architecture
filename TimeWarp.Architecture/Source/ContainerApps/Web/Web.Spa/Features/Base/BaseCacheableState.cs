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
