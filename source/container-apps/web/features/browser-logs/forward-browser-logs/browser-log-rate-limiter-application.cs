#region Purpose
// Singleton token bucket that caps how many browser log entries the endpoint accepts.
#endregion

#region Design
// One global bucket (tokens = entries): the endpoint is anonymous and dev-only with a single
// developer as its client, so per-caller partitioning would add nothing. Empty bucket => 429;
// the JS hook backs off. Capacity 200, refilling 100 entries per 10s, no queueing.
// Not platform/abuse AbuseRateLimitingModule: that is the Production abuse ring (per-IP request
// windows on mint/payment surfaces). This is a dev-only volume cap weighted by entries per batch,
// which a request-counting path limiter cannot express, and it belongs to this slice's lifetime.
#endregion

namespace TimeWarp.Architecture.Features.BrowserLogs.Application;

using System.Threading.RateLimiting;

public sealed class BrowserLogRateLimiter : IDisposable
{
  private readonly TokenBucketRateLimiter Limiter = new(new TokenBucketRateLimiterOptions
  {
    TokenLimit = 200,
    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
    TokensPerPeriod = 100,
    QueueLimit = 0,
    AutoReplenishment = true
  });

  public bool TryAcquire(int entryCount)
  {
    using RateLimitLease lease = Limiter.AttemptAcquire(entryCount);
    return lease.IsAcquired;
  }

  public void Dispose() => Limiter.Dispose();
}
