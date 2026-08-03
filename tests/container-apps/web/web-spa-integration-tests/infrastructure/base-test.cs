#region Purpose
// SPA integration fixture helpers for Jaribu: boot the closed-box Aspire AppHost once (boot
// recipe reused by SpaSessionFixture, task 145-008) and create per-test DI scopes over
// AspireSpaTestApplication.
#endregion

#region Design
// Fixie BaseTest took ISpaTestApplication in the ctor and held one class-long scope. Jaribu has no
// per-class DI. As of task 145-008 the suite shares ONE DistributedApplication across its classes
// via SpaSessionFixture (infrastructure/spa-session-fixture.cs): each class's SetupOnce calls
// SessionFixture.GetAsync&lt;SpaSessionFixture&gt;() instead of StartAsync directly.
// SpaSessionFixture.CreateAsync is the ONLY caller of SpaIntegrationHost.StartAsync — this file
// still owns the boot recipe itself (health waits, ephemeral postgres) so it is written exactly
// once. Disposal is NOT duplicated here either: SpaSessionFixture relies on the generic
// SessionHostFixture&lt;TInner&gt; base (timewarp-testing) whose default DisposeAsync calls
// DistributedApplication.DisposeAsync() directly — there is no separate StopAsync wrapper to
// keep in sync with it.
// SpaTestScope.Create is still called per fact so Store/Sender isolation stays explicit even
// though the underlying AspireSpaTestApplication/DistributedApplication is now shared.
// Full-graph boot matches the pre-migration SpaTestConvention (postgres ephemeral via
// Postgres:UseDataVolume=false). Partial-graph (WithExplicitStart) evaluated under task 145-006 —
// not adopted: the suite needs live web/api/ingress backends (ordering enforced by this host's
// own sequential WaitForResourceHealthyAsync calls; the AppHost itself chains WaitFor only for
// postgres), so pruning grpc is the only cheap win — a small share of boot vs postgres/web;
// leave full graph for fidelity. Session sharing (145-008) cuts the PER-CLASS repetition of that
// cost instead (6 boots -> 1).
#endregion

namespace TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;

using global::Aspire.Hosting;
using global::Aspire.Hosting.Testing;

/// <summary>
/// Starts a closed-box Aspire AppHost for SPA integration tests.
/// </summary>
public static class SpaIntegrationHost
{
  public static async Task<DistributedApplication> StartAsync()
  {
    IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>
      (
        // Ephemeral postgres: test AppHosts must NOT share the deterministic data volume
        // (overlapping instances corrupt its WAL and hang WaitFor - see AppHost Design region).
        ["--Postgres:UseDataVolume=false"]
      );

    DistributedApplication app = await appHost.BuildAsync();
    await app.StartAsync();

    using CancellationTokenSource cts = new(TimeSpan.FromMinutes(2));
    await app.ResourceNotifications.WaitForResourceHealthyAsync("web-server", cts.Token);
#if(api)
    await app.ResourceNotifications.WaitForResourceHealthyAsync("api-server", cts.Token);
#endif
    await app.ResourceNotifications.WaitForResourceHealthyAsync("ingress", cts.Token);

    return app;
  }
}

/// <summary>
/// Per-test scope over an <see cref="ISpaTestApplication"/> ServiceProvider (Store + Sender).
/// </summary>
public sealed class SpaTestScope : IDisposable
{
  private readonly IServiceScope ServiceScope;
  private readonly ISender Sender;

  public IStore Store { get; }

  private SpaTestScope(IServiceScope serviceScope)
  {
    ServiceScope = serviceScope;
    Sender = ServiceScope.ServiceProvider.GetRequiredService<ISender>();
    Store = ServiceScope.ServiceProvider.GetRequiredService<IStore>();
  }

  public static SpaTestScope Create(ISpaTestApplication spaTestApplication)
  {
    IServiceScopeFactory scopeFactory =
      spaTestApplication.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
    return new SpaTestScope(scopeFactory.CreateScope());
  }

  /// <summary>
  /// Dispatch by concrete request type. Do not box as <see cref="IRequest"/> first — that makes
  /// Mediator resolve <c>IRequestHandler&lt;IRequest&gt;</c> and the action is a silent no-op.
  /// </summary>
  public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
    where TRequest : IRequest =>
    Sender.Send(request, cancellationToken);

  public Task<TResponse> Send<TResponse>
  (
    IRequest<TResponse> request,
    CancellationToken cancellationToken = default
  ) =>
    Sender.Send(request, cancellationToken);

  public void Dispose() => ServiceScope.Dispose();
}
