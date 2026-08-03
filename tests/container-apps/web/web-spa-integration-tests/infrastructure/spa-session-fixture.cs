#region Purpose
// Session-scoped wrapper sharing ONE closed-box DistributedApplication across the SPA suite's
// test classes (task 145-008 C-share exemplar), instead of one boot per class (145-006 C-create
// baseline: ~109s wall for 6 booting classes).
#endregion

#region Design
// SessionFixture registration must happen from exactly one ModuleInitializer per process
// (Jaribu's Register throws on double-registration) — kept in this dedicated file, separate
// from each test class's own `RegisterTests<X>()` ModuleInitializer, so adding/removing a test
// class never touches the registration site.
//
// CreateAsync delegates to the EXISTING SpaIntegrationHost.StartAsync — the same boot recipe
// (web/api/ingress health waits, ephemeral postgres) C-create classes called directly before
// this task. No boot logic is duplicated between the per-class and session-scoped shapes (see
// SessionHostFixture<TInner> Design region in timewarp-testing). DisposeAsync is NOT overridden:
// the base's default (Inner.DisposeAsync(), i.e. DistributedApplication.DisposeAsync()) is
// exactly what the old per-class CleanUpOnce called, so there is nothing to add here.
//
// Lazy-create + skip-aware parity (145-006 requirement, still holds under session sharing):
// SessionFixture.GetAsync<SpaSessionFixture>() only creates on first call. The quarantined
// weather-forecast-fetch class (task 058) has no SetupOnce at all — its only fact is [Skip] —
// so it never calls GetAsync and never triggers the shared boot, exactly as before.
#endregion

namespace TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;

using global::Aspire.Hosting;

/// <summary>
/// Session-scoped wrapper around the closed-box Aspire <see cref="DistributedApplication"/> used
/// by SPA integration test classes. Register once via <see cref="Register"/>; resolve per class
/// with <c>SessionFixture.GetAsync&lt;SpaSessionFixture&gt;()</c> from <c>SetupOnce</c>.
/// </summary>
public sealed class SpaSessionFixture : SessionHostFixture<DistributedApplication>
{
  private SpaSessionFixture(DistributedApplication app) : base(app)
  {
  }

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterSessionFixture<SpaSessionFixture>();

  public static async Task<SpaSessionFixture> CreateAsync()
  {
    DistributedApplication app = await SpaIntegrationHost.StartAsync().ConfigureAwait(false);
    return new SpaSessionFixture(app);
  }
}
