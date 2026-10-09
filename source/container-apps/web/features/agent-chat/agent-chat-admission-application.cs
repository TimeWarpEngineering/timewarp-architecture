#region Purpose
// Per-principal concurrency and rate limit for the chat relay, so the endpoint is not an open proxy.
#endregion

#region Design
// Two concurrent calls and thirty per minute, matching CompleteAgentChat's constants. The rate
// permit is consumed at admission; the semaphore is held until the caller disposes the lease.
// WaitAsync is required: a synchronous SemaphoreSlim.Wait faults single-threaded WASM, and this
// type must not grow that pattern even though it runs on the server. The gate dictionary grows
// with distinct principals and is acceptable for the template.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using System.Collections.Concurrent;
using System.Threading.RateLimiting;
using static TimeWarp.Architecture.Features.AgentChats.CompleteAgentChat;

/// <summary>Admits or refuses one completion for a principal.</summary>
public sealed class AgentChatAdmission : IDisposable
{
  private readonly ConcurrentDictionary<string, PrincipalGate> Gates = new(StringComparer.Ordinal);

  /// <summary>
  /// Tries to admit <paramref name="principalId"/>. The returned lease releases the concurrency
  /// slot. <see cref="AdmissionLease.Admitted"/> is false when the principal is at the cap.
  /// </summary>
  public async ValueTask<AdmissionLease> TryAdmitAsync
  (
    string principalId,
    CancellationToken cancellationToken
  )
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(principalId);
    PrincipalGate gate = Gates.GetOrAdd(principalId, static _ => new PrincipalGate());
    if (!await gate.Concurrency.WaitAsync(0, cancellationToken).ConfigureAwait(false))
    {
      return new AdmissionLease(null);
    }

    RateLimitLease rateLease;
    try
    {
      rateLease = await gate.Window.AcquireAsync(1, cancellationToken).ConfigureAwait(false);
    }
    catch
    {
      gate.Concurrency.Release();
      throw;
    }

    if (!rateLease.IsAcquired)
    {
      rateLease.Dispose();
      gate.Concurrency.Release();
      return new AdmissionLease(null);
    }

    rateLease.Dispose();
    return new AdmissionLease(gate.Concurrency);
  }

  /// <summary>Disposes every principal gate.</summary>
  public void Dispose()
  {
    foreach (PrincipalGate gate in Gates.Values)
    {
      gate.Dispose();
    }

    Gates.Clear();
  }

  /// <summary>Holds one principal's concurrency slot until disposed.</summary>
  public sealed class AdmissionLease : IDisposable
  {
    private SemaphoreSlim? Semaphore;

    internal AdmissionLease(SemaphoreSlim? semaphore)
    {
      Semaphore = semaphore;
    }

    /// <summary>True when a slot was taken.</summary>
    public bool Admitted => Semaphore is not null;

    /// <summary>Releases the slot. A second dispose is a no-op.</summary>
    public void Dispose()
    {
      SemaphoreSlim? semaphore = Interlocked.Exchange(ref Semaphore, null);
      semaphore?.Release();
    }
  }

  private sealed class PrincipalGate : IDisposable
  {
    public SemaphoreSlim Concurrency { get; } = new(MaxConcurrentPerPrincipal, MaxConcurrentPerPrincipal);

    public FixedWindowRateLimiter Window { get; } = new
    (
      new FixedWindowRateLimiterOptions
      {
        PermitLimit = MaxRequestsPerMinute,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0,
        AutoReplenishment = true
      }
    );

    public void Dispose()
    {
      Concurrency.Dispose();
      Window.Dispose();
    }
  }
}
