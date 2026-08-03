namespace TimeWarp.Architecture.Testing;

#region Purpose
// C-share wrapper base (task 145-008 / findings §3 C-share): adapts an existing per-class
// factory's product into a Jaribu session-scoped fixture, so a suite can opt a shared host
// instance across its test classes without duplicating boot/dispose logic.
#endregion

#region Design
// Jaribu's SessionFixture contract requires a concrete class with a parameterless
// `public static Task<T> CreateAsync()` (resolved via reflection at RegisterSessionFixture<T>
// registration time) — no delegate/factory injection at the Jaribu layer, by design (upstream
// tasks 029/030: explicit registration, no field-scanning/DI magic). SessionHostFixture<TInner>
// is a thin per-usage base: a suite declares ONE sealed subclass whose CreateAsync calls the
// EXACT SAME factory function its C-create (per-class) callers already call directly, e.g.:
//
//   public sealed class SpaSessionFixture : SessionHostFixture<DistributedApplication>
//   {
//     private SpaSessionFixture(DistributedApplication app) : base(app) { }
//     public static async Task<SpaSessionFixture> CreateAsync() =>
//       new(await SpaIntegrationHost.StartAsync());
//     public override ValueTask DisposeAsync() => SpaIntegrationHost.StopAsync(Inner);
//   }
//
// so boot/teardown logic is written exactly once and reused by both call shapes — no drift
// between "one host per class" and "one host per session". See
// web-spa-integration-tests/infrastructure/spa-session-fixture.cs for the shipped example.
//
// Composition with C-create happens at the REGISTRATION site, not by runtime probing at the
// call site: Jaribu exposes no public "is T registered" query (Register/Clear/IsSessionActive
// are internal to TimeWarp.Jaribu — see SessionFixture there), and catching
// SessionFixture.GetAsync<T>()'s InvalidOperationException to detect "not registered" would be
// unsafe — a legitimate CreateAsync failure inside an ALREADY-registered fixture also throws
// InvalidOperationException (sticky per-session rethrow), so type/message sniffing would
// misclassify a real boot failure as "not registered" and silently double-boot instead of
// surfacing the error. A suite opts in by calling RegisterSessionFixture<T>() exactly once from
// a dedicated ModuleInitializer; module initializers run unconditionally whenever the assembly
// loads, so within one process registration is never partial — every class in that assembly can
// then safely call SessionFixture.GetAsync<T>() directly in SetupOnce. Suites that never
// register a fixture type never call GetAsync either: they keep calling their per-class factory
// (e.g. HostGraphFactory.CreateWebWithApiAsync) and disposing in CleanUpOnce exactly as before —
// this file adds no call sites to their code, so it is zero behavior change for suites that
// don't opt in.
//
// A session-owned instance must NOT be disposed by the consuming class's CleanUpOnce — the
// Jaribu session hook disposes it exactly once when the outermost session ends (MTP
// CloseTestSessionAsync, or the standalone RunTestsAsync-of-one session-of-one wrap). Consuming
// classes null their local reference in CleanUpOnce only.
#endregion

/// <summary>
/// Base for a Jaribu session-scoped fixture that shares an existing per-class-factory-produced
/// instance across a suite's test classes. A sealed subclass implements <c>CreateAsync</c> by
/// delegating to the SAME factory function its non-shared (C-create) callers already use.
/// </summary>
/// <typeparam name="TInner">The shared, disposable resource type (e.g. a distributed app or host graph).</typeparam>
public abstract class SessionHostFixture<TInner> : IAsyncDisposable
  where TInner : IAsyncDisposable
{
  protected SessionHostFixture(TInner inner)
  {
    Inner = inner;
  }

  /// <summary>The shared resource, valid for the lifetime of the owning test session.</summary>
  public TInner Inner { get; }

  /// <summary>
  /// Disposes <see cref="Inner"/>. Override only when teardown needs more than a direct
  /// <c>Inner.DisposeAsync()</c> call (e.g. to reuse an existing named Stop helper).
  /// </summary>
  public virtual ValueTask DisposeAsync()
  {
    GC.SuppressFinalize(this);
    return Inner.DisposeAsync();
  }
}
