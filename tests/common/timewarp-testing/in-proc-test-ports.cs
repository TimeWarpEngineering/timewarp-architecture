#region Purpose
// Single resolved in-proc test port set (web / web-http / api / yarp) for HostGraphFactory.
#endregion

#region Design
// Defaults preserve the historical fixed ports (web=7000, web-http=7001, api=7255, yarp=8443).
// TIMEWARP_TEST_PORT_BASE shifts the whole set by replacing the web HTTPS port; offsets stay
// fixed (+0 / +1 / +255 / +1443) so a single env var keeps the spread without four overrides.
// template-smoke tiers 2–3 set a non-default base so generated-app hosts do not collide with
// monorepo `dev test` on the defaults. `dev test` still serializes projects that share one base.
// Resolved once per process (Lazy); child processes inherit via the environment.
// URL properties stay strings (CA1056 suppressed) — Kestrel UseUrls and YARP DestinationConfig.Address
// take strings; matches the prior const string WebHostUrl surface.
#endregion

namespace TimeWarp.Architecture.Testing;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// In-proc test-lane ports. Override with <see cref="EnvironmentVariableName"/> (web HTTPS port;
/// other ports derive from fixed offsets).
/// </summary>
public static class InProcTestPorts
{
  /// <summary>Environment variable: web HTTPS port; default <see cref="DefaultBase"/>.</summary>
  public const string EnvironmentVariableName = "TIMEWARP_TEST_PORT_BASE";

  /// <summary>Default web HTTPS port (historical fixed port).</summary>
  public const int DefaultBase = 7000;

  private const int WebHttpOffset = 1;
  private const int ApiOffset = 255;
  private const int YarpOffset = 1443;

  private static readonly Lazy<ResolvedPorts> Resolved =
    new(Resolve, LazyThreadSafetyMode.ExecutionAndPublication);

  /// <summary>Web.Server HTTPS port (base).</summary>
  public static int WebPort => Resolved.Value.WebPort;

  /// <summary>Web.Server HTTP port (YARP Development hop).</summary>
  public static int WebHttpPort => Resolved.Value.WebHttpPort;

  /// <summary>Api.Server HTTPS port.</summary>
  public static int ApiPort => Resolved.Value.ApiPort;

  /// <summary>YARP gateway HTTPS port.</summary>
  public static int YarpPort => Resolved.Value.YarpPort;

  /// <summary>Web.Server HTTPS URL.</summary>
  [SuppressMessage(
    "Design",
    "CA1056:URI-like properties should not be strings",
    Justification = "Kestrel UseUrls and YARP DestinationConfig.Address take strings.")]
  public static string WebHostUrl => Resolved.Value.WebHostUrl;

  /// <summary>Web.Server HTTP URL.</summary>
  [SuppressMessage(
    "Design",
    "CA1056:URI-like properties should not be strings",
    Justification = "Kestrel UseUrls and YARP DestinationConfig.Address take strings.")]
  public static string WebHttpUrl => Resolved.Value.WebHttpUrl;

  /// <summary>Api.Server HTTPS URL.</summary>
  [SuppressMessage(
    "Design",
    "CA1056:URI-like properties should not be strings",
    Justification = "Kestrel UseUrls and YARP DestinationConfig.Address take strings.")]
  public static string ApiHostUrl => Resolved.Value.ApiHostUrl;

  /// <summary>YARP gateway HTTPS URL.</summary>
  [SuppressMessage(
    "Design",
    "CA1056:URI-like properties should not be strings",
    Justification = "Kestrel UseUrls and YARP DestinationConfig.Address take strings.")]
  public static string YarpHostUrl => Resolved.Value.YarpHostUrl;

  private static ResolvedPorts Resolve()
  {
    int basePort = DefaultBase;
    string? raw = Environment.GetEnvironmentVariable(EnvironmentVariableName);
    if (!string.IsNullOrWhiteSpace(raw))
    {
      if (!int.TryParse(raw, out basePort) || basePort is < 1 or > 65535 - YarpOffset)
      {
        throw new InvalidOperationException(
          $"{EnvironmentVariableName} must be an integer port in [1, {65535 - YarpOffset}] " +
          $"(got '{raw}'); yarp uses base+{YarpOffset}.");
      }
    }

    int web = basePort;
    int webHttp = basePort + WebHttpOffset;
    int api = basePort + ApiOffset;
    int yarp = basePort + YarpOffset;

    return new ResolvedPorts(
      web,
      webHttp,
      api,
      yarp,
      $"https://localhost:{web}",
      $"http://localhost:{webHttp}",
      $"https://localhost:{api}",
      $"https://localhost:{yarp}");
  }

  private readonly record struct ResolvedPorts(
    int WebPort,
    int WebHttpPort,
    int ApiPort,
    int YarpPort,
    string WebHostUrl,
    string WebHttpUrl,
    string ApiHostUrl,
    string YarpHostUrl);
}
