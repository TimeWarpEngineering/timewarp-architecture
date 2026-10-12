#region Purpose
// The one way a closed-box suite boots the real AppHost in run mode: standard args (ephemeral
// postgres, dynamic ingress ports) plus suite-specific extras.
#endregion

#region Design
// Task 319: DistributedApplicationTestingBuilder loads the AppHost's appsettings.Development.json,
// which pins the ingress to Ingress:Port 63610 / Ingress:HttpPort 63620 — the maintainer's
// `dev run` ports. A test AppHost that binds them collides with a running dev instance and its
// requests can reach that instance. The AppHost applies a pin only when the value parses as an
// int, so an empty command-line override (`--Ingress:Port=`) clears it and Aspire assigns ports.
// Tests resolve the ingress through the running app (CreateHttpClient / GetEndpoint), never by
// port number.
// Ephemeral postgres: test AppHosts must NOT share the deterministic data volume (overlapping
// instances corrupt its WAL and hang WaitFor — see AppHost Design region).
// Shared as one source file linked into each closed-box suite (Compile Include … Link), not a
// project: api-server-integration-tests must not load timewarp-testing (FastEndpoints discovery
// pollution, task 145-005), and timewarp-testing has no Aspire.Hosting.Testing reference.
// Publish-mode boots (`--operation publish`) bind no ports and do not use this helper.
// Guarded by aspire-tests/closed-box-app-host-guard-tests.cs.
#endregion

namespace TimeWarp.Architecture.Testing;

using global::Aspire.Hosting.Testing;

/// <summary>
/// Creates the closed-box run-mode AppHost testing builder with the standard test arguments.
/// </summary>
public static class ClosedBoxAppHost
{
  /// <summary>
  /// Standard closed-box AppHost arguments followed by <paramref name="extraArgs"/>.
  /// </summary>
  public static string[] Args(params string[] extraArgs) =>
  [
    "--Postgres:UseDataVolume=false",
    "--Ingress:Port=",
    "--Ingress:HttpPort=",
    .. extraArgs
  ];

  /// <summary>
  /// Creates the AppHost testing builder with <see cref="Args"/>.
  /// </summary>
  public static Task<IDistributedApplicationTestingBuilder> CreateAsync(params string[] extraArgs) =>
    DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>(Args(extraArgs));
}
