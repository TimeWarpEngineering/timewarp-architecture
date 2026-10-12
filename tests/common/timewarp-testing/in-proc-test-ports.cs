#region Purpose
// Single resolved in-proc test port set (web / web-http / api / yarp) for HostGraphFactory.
#endregion

#region Design
// One base port plus fixed offsets: web = base, web-http = +1, api = +255, yarp = +1443, and
// yarp-http is the next port (+1444). A single env var keeps the spread without four overrides.
//
// Base precedence (task 330), resolved once per process (Lazy):
//   1. TIMEWARP_TEST_PORT_BASE set → that port, exactly (Override).
//   2. Otherwise derive from the checkout root — the nearest directory holding a *.slnx — with a
//      stable FNV-1a hash of the normalized absolute path (Derived). Bases land in
//      [DerivedRangeStart, DerivedRangeEnd) on DerivedStep boundaries, so two different bases never
//      share a port (step ≥ the 1444 spread) and the whole set stays below 40000, clear of the
//      7000-range defaults other tools pick and of the Windows ephemeral range (49152+).
//   3. No *.slnx found → LegacyBase 7000 (Fallback).
// Why per checkout: parallel workers each run `dev test` in their own worktree; a fixed 7000 made
// two worktrees collide. Different checkouts get different bases (13 slots — not a guarantee, so
// HostGraphFactory.EnsurePortIsFree fails loudly with Describe() and the override variable rather
// than silently moving a port that child processes must agree on).
// Root search starts at the runfile's source directory (EntryPointFileDirectoryPath — Jaribu
// runfiles build under the runfile cache, not the checkout), then AppContext.BaseDirectory
// (project builds under the repo's bin/artifacts), then the current directory. The derivation
// lives here, not in `dev test`, so plain `dotnet test`/`dotnet run` and runfiles get it too.
// `dev test` and template-smoke compile this file in (tools/dev-cli/Directory.Build.props) and
// export the resolved base to every child, so one source decides the number.
// No ganda dependency: the hash is self-contained (ganda is private).
// URL properties stay strings (CA1056 suppressed) — Kestrel UseUrls and YARP DestinationConfig.Address
// take strings; matches the prior const string WebHostUrl surface.
#endregion

namespace TimeWarp.Architecture.Testing;

using System.Diagnostics.CodeAnalysis;
using System.Text;

/// <summary>
/// In-proc test-lane ports. Override with <see cref="EnvironmentVariableName"/> (web HTTPS port;
/// other ports derive from fixed offsets); otherwise the base is derived from the checkout path.
/// </summary>
public static class InProcTestPorts
{
  /// <summary>Environment variable: web HTTPS port; wins over the derived base.</summary>
  public const string EnvironmentVariableName = "TIMEWARP_TEST_PORT_BASE";

  /// <summary>Base used when no checkout root (a directory holding a *.slnx) is found.</summary>
  public const int LegacyBase = 7000;

  /// <summary>Lowest derived base.</summary>
  public const int DerivedRangeStart = 20000;

  /// <summary>Every derived port (base + <see cref="PortSpan"/>) stays below this.</summary>
  public const int DerivedRangeEnd = 40000;

  /// <summary>Derived bases are DerivedRangeStart + n * DerivedStep; step exceeds the spread.</summary>
  public const int DerivedStep = 1500;

  /// <summary>Highest offset used from the base (yarp-http).</summary>
  public const int PortSpan = YarpOffset + 1;

  /// <summary>Number of distinct derived bases.</summary>
  public const int DerivedSlotCount = ((DerivedRangeEnd - 1 - PortSpan - DerivedRangeStart) / DerivedStep) + 1;

  private const int WebHttpOffset = 1;
  private const int ApiOffset = 255;
  private const int YarpOffset = 1443;

  private static readonly Lazy<ResolvedPorts> Resolved =
    new(Resolve, LazyThreadSafetyMode.ExecutionAndPublication);

  /// <summary>Where the base came from.</summary>
  public enum BaseSource
  {
    /// <summary><see cref="EnvironmentVariableName"/> was set.</summary>
    Override,

    /// <summary>Hashed from the checkout root path.</summary>
    Derived,

    /// <summary>No checkout root found; <see cref="LegacyBase"/>.</summary>
    Fallback,
  }

  /// <summary>Resolved base port (web HTTPS).</summary>
  public static int BasePort => Resolved.Value.WebPort;

  /// <summary>How <see cref="BasePort"/> was chosen.</summary>
  public static BaseSource Source => Resolved.Value.Source;

  /// <summary>Checkout root used for derivation, when found.</summary>
  public static string? CheckoutRoot => Resolved.Value.CheckoutRoot;

  /// <summary>Web.Server HTTPS port (base).</summary>
  public static int WebPort => Resolved.Value.WebPort;

  /// <summary>Web.Server HTTP port (YARP Development hop).</summary>
  public static int WebHttpPort => Resolved.Value.WebHttpPort;

  /// <summary>Api.Server HTTPS port.</summary>
  public static int ApiPort => Resolved.Value.ApiPort;

  /// <summary>YARP gateway HTTPS port.</summary>
  public static int YarpPort => Resolved.Value.YarpPort;

  /// <summary>YARP gateway HTTP port (trusted-proxy scheme tests; the next port after <see cref="YarpPort"/>).</summary>
  public static int YarpHttpPort => YarpPort + 1;

  /// <summary>Web.Server HTTPS URL.</summary>
  [SuppressMessage(
    "Design",
    "CA1056:URI-like properties should not be strings",
    Justification = "Kestrel UseUrls and YARP DestinationConfig.Address take strings.")]
  public static string WebHostUrl => $"https://localhost:{WebPort}";

  /// <summary>Web.Server HTTP URL.</summary>
  [SuppressMessage(
    "Design",
    "CA1056:URI-like properties should not be strings",
    Justification = "Kestrel UseUrls and YARP DestinationConfig.Address take strings.")]
  public static string WebHttpUrl => $"http://localhost:{WebHttpPort}";

  /// <summary>Api.Server HTTPS URL.</summary>
  [SuppressMessage(
    "Design",
    "CA1056:URI-like properties should not be strings",
    Justification = "Kestrel UseUrls and YARP DestinationConfig.Address take strings.")]
  public static string ApiHostUrl => $"https://localhost:{ApiPort}";

  /// <summary>YARP gateway HTTPS URL.</summary>
  [SuppressMessage(
    "Design",
    "CA1056:URI-like properties should not be strings",
    Justification = "Kestrel UseUrls and YARP DestinationConfig.Address take strings.")]
  public static string YarpHostUrl => $"https://localhost:{YarpPort}";

  /// <summary>YARP gateway HTTP URL.</summary>
  [SuppressMessage(
    "Design",
    "CA1056:URI-like properties should not be strings",
    Justification = "Kestrel UseUrls and YARP DestinationConfig.Address take strings.")]
  public static string YarpHttpUrl => $"http://localhost:{YarpHttpPort}";

  /// <summary>One-line provenance of the resolved base, for errors and logs.</summary>
  public static string Describe() =>
    Describe(Resolved.Value.WebPort, Resolved.Value.Source, Resolved.Value.CheckoutRoot);

  /// <summary>One-line provenance of a base, for errors and logs.</summary>
  public static string Describe(int basePort, BaseSource source, string? checkoutRoot) => source switch
  {
    BaseSource.Override when checkoutRoot is not null =>
      $"base {basePort} from {EnvironmentVariableName} (checkout {checkoutRoot} derives {DeriveBase(checkoutRoot)}; " +
      "`dev test` exports the derived base)",
    BaseSource.Override => $"base {basePort} from {EnvironmentVariableName}",
    BaseSource.Derived =>
      $"base {basePort} derived from checkout {checkoutRoot} (set {EnvironmentVariableName} to override)",
    BaseSource.Fallback =>
      $"base {basePort} (no *.slnx checkout root found; set {EnvironmentVariableName} to override)",
    _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
  };

  /// <summary>
  /// Pure resolution: <paramref name="overrideValue"/> (the env var's raw value) wins; else the
  /// derived base for <paramref name="checkoutRoot"/>; else <see cref="LegacyBase"/>.
  /// </summary>
  public static (int BasePort, BaseSource Source) ResolveBase(string? overrideValue, string? checkoutRoot)
  {
    if (!string.IsNullOrWhiteSpace(overrideValue))
    {
      if (!int.TryParse(overrideValue, out int basePort) || basePort is < 1 or > 65535 - PortSpan)
      {
        throw new InvalidOperationException(
          $"{EnvironmentVariableName} must be an integer port in [1, {65535 - PortSpan}] " +
          $"(got '{overrideValue}'); yarp uses base+{YarpOffset} and the next port.");
      }

      return (basePort, BaseSource.Override);
    }

    return checkoutRoot is null
      ? (LegacyBase, BaseSource.Fallback)
      : (DeriveBase(checkoutRoot), BaseSource.Derived);
  }

  /// <summary>Stable base for a checkout root path (FNV-1a over the normalized absolute path).</summary>
  public static int DeriveBase(string checkoutRoot)
  {
    string normalized = Path.GetFullPath(checkoutRoot)
      .Replace('\\', '/')
      .TrimEnd('/');
    if (OperatingSystem.IsWindows())
      normalized = normalized.ToUpperInvariant();

    uint hash = 2166136261;
    foreach (byte b in Encoding.UTF8.GetBytes(normalized))
    {
      hash ^= b;
      hash *= 16777619;
    }

    return DerivedRangeStart + ((int)(hash % DerivedSlotCount) * DerivedStep);
  }

  /// <summary>The next derived slot after <paramref name="basePort"/> (wraps).</summary>
  public static int NextDerivedBase(int basePort)
  {
    int slot = (basePort - DerivedRangeStart) / DerivedStep;
    return DerivedRangeStart + (((slot + 1) % DerivedSlotCount) * DerivedStep);
  }

  /// <summary>Nearest ancestor (inclusive) of any start directory that holds a *.slnx.</summary>
  public static string? FindCheckoutRoot(IEnumerable<string?> startDirectories)
  {
    ArgumentNullException.ThrowIfNull(startDirectories);
    foreach (string? start in startDirectories)
    {
      if (string.IsNullOrWhiteSpace(start))
        continue;

      DirectoryInfo? directory = new(Path.GetFullPath(start));
      while (directory is not null)
      {
        if (directory.Exists && directory.EnumerateFiles("*.slnx").Any())
          return directory.FullName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        directory = directory.Parent;
      }
    }

    return null;
  }

  private static ResolvedPorts Resolve()
  {
    string? raw = Environment.GetEnvironmentVariable(EnvironmentVariableName);
    // Found even under an override so collision messages can name the checkout.
    string? checkoutRoot = FindCheckoutRoot(
      [
        AppContext.GetData("EntryPointFileDirectoryPath") as string,
        AppContext.BaseDirectory,
        Environment.CurrentDirectory,
      ]);

    (int basePort, BaseSource source) = ResolveBase(raw, checkoutRoot);

    return new ResolvedPorts(
      basePort,
      basePort + WebHttpOffset,
      basePort + ApiOffset,
      basePort + YarpOffset,
      source,
      checkoutRoot);
  }

  private readonly record struct ResolvedPorts(
    int WebPort,
    int WebHttpPort,
    int ApiPort,
    int YarpPort,
    BaseSource Source,
    string? CheckoutRoot);
}
