#region Purpose
// Shared config keys / header name / fail-closed environment gate for runtime mock auth
// (task 145-009). Foundation contracts so SPA, web-server, and host-free tests share one SSOT.
#endregion

#region Design
// Fail-closed: mock paths require Development or Testing AND Authentication:UseMock=true.
// Production never activates mock auth when the flag is set. Absent flag defaults false.
// appsettings.Development.json sets UseMock=true so local template UX matches the old
// compile-time mock-auth define. No dependency on Hosting abstractions — environment names
// are compared as plain strings.
#endregion

namespace TimeWarp.Architecture.Configuration;

/// <summary>
/// Shared defaults for runtime-config-gated mock authentication (SPA + closed-box BFF).
/// </summary>
public static class MockAuthenticationDefaults
{
  /// <summary>
  /// When true (and environment is Development/Testing), enable mock SPA auth providers and
  /// the web-server mock-principal middleware. Default false when the key is absent.
  /// </summary>
  public const string UseMockKey = "Authentication:UseMock";

  /// <summary>
  /// Request header that, when mock mode is active, establishes an identity-session principal
  /// for closed-box HTTP tests without a passkey ceremony.
  /// </summary>
  public const string MockPrincipalIdHeader = "X-TimeWarp-Mock-Principal-Id";

  /// <summary>
  /// Returns true only for environments that may activate mock authentication.
  /// </summary>
  public static bool IsMockEnvironmentAllowed(string? environmentName)
  {
    if (string.IsNullOrEmpty(environmentName))
      return false;

    return string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase)
      || string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Fail-closed activation predicate: environment allow-list AND config flag true.
  /// Uses indexer lookup (no ConfigurationBinder package dependency on foundation-contracts).
  /// </summary>
  public static bool IsMockAuthActive(string? environmentName, IReadOnlyDictionary<string, string?> configuration)
  {
    if (!IsMockEnvironmentAllowed(environmentName))
      return false;

    if (!configuration.TryGetValue(UseMockKey, out string? value) || value is null)
      return false;

    return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
      || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Fail-closed activation for <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>.
  /// </summary>
  public static bool IsMockAuthActive(string? environmentName, Microsoft.Extensions.Configuration.IConfiguration configuration)
  {
    if (!IsMockEnvironmentAllowed(environmentName))
      return false;

    string? value = configuration[UseMockKey];
    if (string.IsNullOrEmpty(value))
      return false;

    return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
      || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
  }
}
