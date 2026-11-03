#region Purpose
// Reference-counted lease over one HostGraph so several split test classes can share the arrange helpers
// that read the host while each class keeps the C-share lifetime (SetupOnce / CleanUpOnce) of the original.
#endregion

#region Design
// The first AcquireAsync builds the graph; the last ReleaseAsync disposes it. Classes that run one after
// another each get a fresh host as before; classes whose lifetimes overlap share one. A semaphore
// serializes acquire / release so the count and graph never tear.
#endregion

namespace TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;

internal sealed class HostGraphLease : IDisposable
{
  private readonly SemaphoreSlim Gate = new(1, 1);
  private HostGraph? Graph;
  private int Leases;

  public void Dispose() => Gate.Dispose();

  public HostGraph Current => Graph ?? throw new InvalidOperationException("HostGraphLease has no active lease.");

  public async Task AcquireAsync(Func<Task<HostGraph>> create)
  {
    await Gate.WaitAsync();
    try
    {
      if (Leases == 0)
      {
        Graph = await create();
      }

      Leases++;
    }
    finally
    {
      Gate.Release();
    }
  }

  public async Task ReleaseAsync()
  {
    await Gate.WaitAsync();
    try
    {
      if (Leases == 0)
      {
        return;
      }

      Leases--;
      if (Leases == 0 && Graph is not null)
      {
        await Graph.DisposeAsync();
        Graph = null;
      }
    }
    finally
    {
      Gate.Release();
    }
  }
}
