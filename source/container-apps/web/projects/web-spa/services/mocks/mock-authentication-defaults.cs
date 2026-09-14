#region Purpose
// Shared Authentication:* config keys and fail-closed gates for mock auth (145-009) and
// named Entra OIDC (RFC 219 D10). Lives in Web.Spa so template sourceName rewrite keeps SPA +
// web-server (which ProjectReferences Web.Spa) aligned without a Foundation package version race.
#endregion

#region Design
// Fail-closed mock: Development/Testing AND Authentication:UseMock=true. Production never
// activates mock when the flag is set. Absent flags default false.
// Entra: Authentication:Entra:Enabled registers a named OIDC scheme; identity-session stays
// DefaultScheme. Obsolete Authentication:UseEntra maps to Enabled for one version and MUST
// never restore default-scheme Entra. SPA session is always identity-session when not mock
// (no WASM MSAL). Callers pass raw config strings so this type has no IConfiguration dependency.
// X-TimeWarp-Circuit-Host is set only by IdentitySessionCookieForwardingHandler from the circuit
// request Host (port stripped). Same trust as the mock principal header: never copied from a
// client-supplied X-Forwarded-Host. HttpRequestHostAccessor honors it only when Request.Host is
// loopback; on the public path a client-supplied copy is ignored.
#endregion

namespace TimeWarp.Architecture.Services;

/// <summary>
/// Shared defaults for runtime-config-gated mock authentication (SPA + closed-box BFF)
/// and named Entra OIDC enablement (RFC 219 D10).
/// </summary>
public static class MockAuthenticationDefaults
{
  /// <summary>
  /// When true (and environment is Development/Testing), enable mock SPA auth providers and
  /// the web-server mock-principal scheme. Default false when the key is absent.
  /// </summary>
  public const string UseMockKey = "Authentication:UseMock";

  /// <summary>
  /// When true, register the named <c>entra</c> OIDC scheme. identity-session stays DefaultScheme.
  /// </summary>
  public const string EntraEnabledKey = "Authentication:Entra:Enabled";

  /// <summary>
  /// Obsolete synonym for <see cref="EntraEnabledKey"/>. Accepted for one template version;
  /// never selects Entra as DefaultScheme.
  /// </summary>
  public const string UseEntraKey = "Authentication:UseEntra";

  /// <summary>
  /// Request header that, when mock mode is active, establishes an identity-session principal
  /// for closed-box HTTP tests without a passkey ceremony.
  /// </summary>
  public const string MockPrincipalIdHeader = "X-TimeWarp-Mock-Principal-Id";

  /// <summary>
  /// Internal header set only by IdentitySessionCookieForwardingHandler so HTTPS loopback
  /// can carry the circuit/page host for WebAuthn RP-ID selection without rewriting HTTP Host
  /// (which HttpClient uses for TLS SNI / certificate name validation against localhost).
  /// HttpRequestHostAccessor honors it only when Request.Host is loopback.
  /// Never read a client-supplied X-Forwarded-Host in its place.
  /// </summary>
  public const string CircuitHostHeader = "X-TimeWarp-Circuit-Host";

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
  /// Named Entra scheme: <paramref name="entraEnabledValue"/> true/1, else obsolete
  /// <paramref name="useEntraValue"/> true/1. Absent is false. Never means DefaultScheme Entra.
  /// </summary>
  public static bool IsEntraEnabled(string? entraEnabledValue, string? useEntraValue) =>
    IsTruthy(entraEnabledValue) || IsTruthy(useEntraValue);

  /// <summary>
  /// True when Enabled comes only from the obsolete <see cref="UseEntraKey"/> synonym.
  /// </summary>
  public static bool UsedObsoleteUseEntraKey(string? entraEnabledValue, string? useEntraValue) =>
    !IsTruthy(entraEnabledValue) && IsTruthy(useEntraValue);

  /// <summary>
  /// Opt-in Entra: true/1 only. Absent or any other value is false (non-default).
  /// Prefer <see cref="IsEntraEnabled"/>; this overload remains for the obsolete key's raw value.
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
