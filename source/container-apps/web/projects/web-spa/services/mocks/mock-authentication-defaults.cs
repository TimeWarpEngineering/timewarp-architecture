#region Purpose
// Shared Authentication:* config keys and fail-closed gates for mock auth (145-009) and
// optional Entra/MSAL (104-021). Lives in Web.Spa so template sourceName rewrite keeps SPA +
// web-server (which ProjectReferences Web.Spa) aligned without a Foundation package version race.
#endregion

#region Design
// Fail-closed mock: Development/Testing AND Authentication:UseMock=true. Production never
// activates mock when the flag is set. Absent flags default false.
// Entra (Authentication:UseEntra): opt-in only — default happy path is mock (dev) or first-party
// identity-session / passkey (non-mock). MSAL and AzureAd* appsettings are dormant unless true.
// Callers pass raw config strings so this type has no IConfiguration dependency.
#endregion

namespace TimeWarp.Architecture.Services;

/// <summary>
/// Shared defaults for runtime-config-gated mock authentication (SPA + closed-box BFF)
/// and optional Entra/MSAL opt-in (task 104-021).
/// </summary>
public static class MockAuthenticationDefaults
{
  /// <summary>
  /// When true (and environment is Development/Testing), enable mock SPA auth providers and
  /// the web-server mock-principal scheme. Default false when the key is absent.
  /// </summary>
  public const string UseMockKey = "Authentication:UseMock";

  /// <summary>
  /// When true, register Microsoft Entra / MSAL as the SPA + web-server auth path.
  /// Default false — passkey identity-session is the non-mock happy path (task 104-021).
  /// </summary>
  public const string UseEntraKey = "Authentication:UseEntra";

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
  /// Fail-closed activation: environment allow-list AND UseMock flag value true/1.
  /// </summary>
  public static bool IsMockAuthActive(string? environmentName, string? useMockConfigurationValue)
  {
    if (!IsMockEnvironmentAllowed(environmentName))
      return false;

    return IsTruthy(useMockConfigurationValue);
  }

  /// <summary>
  /// Opt-in Entra/MSAL: true/1 only. Absent or any other value is false (non-default).
  /// </summary>
  public static bool IsEntraAuthActive(string? useEntraConfigurationValue) =>
    IsTruthy(useEntraConfigurationValue);

  private static bool IsTruthy(string? configurationValue)
  {
    if (string.IsNullOrEmpty(configurationValue))
      return false;

    return string.Equals(configurationValue, "true", StringComparison.OrdinalIgnoreCase)
      || string.Equals(configurationValue, "1", StringComparison.OrdinalIgnoreCase);
  }
}
