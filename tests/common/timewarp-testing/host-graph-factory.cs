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
//   Web+Api:  Api first (BFF HttpClient bases point at :7255), then Web :7000
//   Full:     Api, Web, then Yarp :8443 (Yarp depends on the others for DI identity only)
// Port preflight fails with a teaching error so parallel/leaked hosts are obvious.
// Per-host Action<IServiceCollection> runs after each host's built-in test wiring
// (Web already registers MockAccessTokenProvider).
// Host constructors start Kestrel synchronously; async methods still use await for dispose
// rollback and a consistent SetupOnce await shape.
#endregion

/// <summary>
/// Explicit ordered construction of in-proc test hosts for C-create Jaribu SetupOnce usage.
/// </summary>
[NotTest]
public static class HostGraphFactory
{
  /// <summary>Api.Server only (fixed port 7255).</summary>
  public static async Task<HostGraph> CreateApiAsync(Action<IServiceCollection>? configureApi = null)
  {
    EnsurePortIsFree(ApiTestServerApplication.ApiPort, "Api.Server (ApiTestServerApplication)");
    ApiTestServerApplication api = new(configureApi);
    await Task.CompletedTask.ConfigureAwait(false);
    return new HostGraph { Api = api };
  }

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
