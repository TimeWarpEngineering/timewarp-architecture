namespace TimeWarp.Architecture.Testing;

using System.Net;
using System.Net.Sockets;

#region Purpose
// C-create factory: builds correctly ordered, per-class-owned in-proc host graphs for Jaribu tests.
#endregion

#region Design
// Replaces the implicit Fixie DI graph (AddSingleton hosts + ctor injection) with explicit
// Create* methods (task 145-002 / findings §3 C-create). Each call constructs NEW hosts —
// never process-static, never refcounted. Ordering:
//   Api-only: Api :7255
//   Web-only: Web :7000
//   Web+Api:  Api first (BFF HttpClient bases point at :7255), then Web :7000
//   Full:     Api, Web, then Yarp :8443 (Yarp depends on the others for DI identity only)
// Port preflight fails with a teaching error so parallel/leaked hosts are obvious.
// Per-host Action<IServiceCollection> runs after each host's built-in test wiring
// (Web already registers MockAccessTokenProvider).
// Host constructors start Kestrel synchronously; async methods still use await for dispose
// rollback and a consistent SetupOnce await shape.
// Per-family conditional-compilation guards (task 145-002 R2-1 fix follow-up, template-smoke SmokeNoApi regression):
// same reasoning as HostGraph — this file ships unconditionally, so each Create* method that
// names a family-specific type must be guarded to the family combination it needs.
// CreateWebAsync (task 145-004 R2-1): added because web-server-integration-tests' 19 classes
// (21 call sites) all call CreateWebWithApiAsync but NONE of them actually touch Graph.Api or
// any Api-backed HttpClient (verified: zero `.Api` references, zero HttpClient/ApiServiceName
// usage in the handlers under test — admin/roles, analytics/track-event, hello, identity are
// all self-contained in Web.Server). The suite inherited "boot everything" from the pre-migration
// Fixie convention (findings §1: the old "AspiredApp" fixture built the full distributed app for
// every consumer regardless of need), not from a real Api dependency. Call sites now branch on
// the api template flag (CreateWebWithApiAsync when present, CreateWebAsync otherwise, guarded
// via the api-flag conditional-compilation directive pair) so `--api false` degrades to a
// Web-only host instead of failing to compile (CS0117 x21) — this preserves current behavior
// when api is present (still boots both, matching the reviewed/shipped topology) while adding
// the web-only compile+run coverage the Fixie-era suite always had.
#endregion

/// <summary>
/// Explicit ordered construction of in-proc test hosts for C-create Jaribu SetupOnce usage.
/// </summary>
public static class HostGraphFactory
{
#if(api)
  /// <summary>Api.Server only (fixed port 7255).</summary>
  public static async Task<HostGraph> CreateApiAsync(Action<IServiceCollection>? configureApi = null)
  {
    EnsurePortIsFree(ApiTestServerApplication.ApiPort, "Api.Server (ApiTestServerApplication)");
    ApiTestServerApplication api = new(configureApi);
    await Task.CompletedTask.ConfigureAwait(false);
    return new HostGraph { Api = api };
  }
#endif
#if(web)

  /// <summary>
  /// Web.Server only (fixed port 7000). Used when the api family is absent — see
  /// web-server-integration-tests call sites' api-flag-guarded branch (task 145-004 R2-1:
  /// SmokeNoApi regression) — and by any class that genuinely doesn't need a live Api host.
  /// </summary>
  public static async Task<HostGraph> CreateWebAsync(Action<IServiceCollection>? configureWeb = null)
  {
    EnsurePortIsFree(WebTestServerApplication.WebPort, "Web.Server (WebTestServerApplication)");
    WebTestServerApplication web = new(configureWeb);
    await Task.CompletedTask.ConfigureAwait(false);
    return new HostGraph { Web = web };
  }
#endif
#if(web && api)

  /// <summary>
  /// Api then Web (ports 7255, 7000). Web's built-in wiring includes MockAccessTokenProvider
  /// and HttpClient base addresses for BFF → Api.
  /// </summary>
  public static async Task<HostGraph> CreateWebWithApiAsync
  (
    Action<IServiceCollection>? configureApi = null,
    Action<IServiceCollection>? configureWeb = null
  )
  {
    EnsurePortIsFree(ApiTestServerApplication.ApiPort, "Api.Server (ApiTestServerApplication)");
    EnsurePortIsFree(WebTestServerApplication.WebPort, "Web.Server (WebTestServerApplication)");

    // Api first: Web BFF clients default to https://localhost:7255.
    ApiTestServerApplication api = new(configureApi);
    try
    {
      WebTestServerApplication web = new(configureWeb);
      await Task.CompletedTask.ConfigureAwait(false);
      return new HostGraph { Api = api, Web = web };
    }
    catch
    {
      await api.DisposeAsync().ConfigureAwait(false);
      throw;
    }
  }
#endif
#if(web && api && yarp)

  /// <summary>Api, Web, then Yarp (ports 7255, 7000, 8443).</summary>
  public static async Task<HostGraph> CreateWebApiYarpAsync
  (
    Action<IServiceCollection>? configureApi = null,
    Action<IServiceCollection>? configureWeb = null,
    Action<IServiceCollection>? configureYarp = null
  )
  {
    EnsurePortIsFree(ApiTestServerApplication.ApiPort, "Api.Server (ApiTestServerApplication)");
    EnsurePortIsFree(WebTestServerApplication.WebPort, "Web.Server (WebTestServerApplication)");
    EnsurePortIsFree(YarpTestServerApplication.YarpPort, "Yarp (YarpTestServerApplication)");

    ApiTestServerApplication api = new(configureApi);
    WebTestServerApplication? web = null;
    try
    {
      web = new(configureWeb);
      YarpTestServerApplication yarp = new(web, api, configureYarp);
      await Task.CompletedTask.ConfigureAwait(false);
      return new HostGraph { Api = api, Web = web, Yarp = yarp };
    }
    catch
    {
      if (web is not null)
        await web.DisposeAsync().ConfigureAwait(false);
      await api.DisposeAsync().ConfigureAwait(false);
      throw;
    }
  }
#endif

  /// <summary>
  /// Fails if a fixed test port is already bound — usually another suite or a leaked host.
  /// <c>dev test</c> runs projects one at a time for this reason.
  /// </summary>
  public static void EnsurePortIsFree(int port, string hostLabel)
  {
    try
    {
      using TcpListener listener = new(IPAddress.Loopback, port);
      listener.Start();
      listener.Stop();
    }
    catch (SocketException exception)
    {
      throw new InvalidOperationException(
        $"Fixed test port {port} for {hostLabel} is already in use. " +
        "Stop the other process (or finish its CleanUpOnce/DisposeAsync). " +
        "`dev test` runs test projects serially so ports are not shared across projects; " +
        "within a project each Jaribu class must own and dispose its HostGraph (C-create).",
        exception);
    }
  }
}
