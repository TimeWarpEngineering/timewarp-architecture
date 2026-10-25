#region Purpose
// Typed outcome of a domain to Entra tenant-id lookup.
#endregion

#region Design
// Each failure the admin can see is its own value. Callers must not collapse these to null.
// Found and NotFound are the only outcomes EntraTenantResolver caches.
#endregion

namespace TimeWarp.Identity;

/// <summary>Result of resolving a domain to an Entra tenant id.</summary>
public enum EntraTenantLookupOutcome
{
  /// <summary>The issuer GUID was read from a successful discovery document.</summary>
  Found,

  /// <summary>Microsoft reported that the domain is not a tenant.</summary>
  NotFound,

  /// <summary>The domain failed <see cref="EntraTenantIds.TryNormalizeDomain"/> before any HTTP call.</summary>
  InvalidDomain,

  /// <summary>The HTTP call failed before a response.</summary>
  NetworkFailure,

  /// <summary>The HTTP call exceeded the lookup timeout.</summary>
  Timeout,

  /// <summary>Microsoft returned a status that is neither success nor an unknown tenant.</summary>
  UnexpectedStatus,

  /// <summary>The discovery document's issuer was missing or not a v2 tenant issuer.</summary>
  UnexpectedIssuer,
}
