#region Purpose
// One accepted Entra tenant: the tid to match at sign-in, plus the domain an admin looked up.
#endregion

#region Design
// A pasted GUID has no domain. A domain lookup stores both. Duplicates are keyed by tenant id
// in EntraTenantIds.TryNormalize, which also owns domain shape. This record does not validate:
// callers pass raw input through TryNormalize so an empty GUID or a bad domain is a result,
// not a constructor throw.
#endregion

namespace TimeWarp.Identity;

/// <summary>An accepted Entra tenant id and the optional primary domain it was resolved from.</summary>
public sealed record EntraAcceptedTenant(Guid TenantId, string? Domain)
{
  /// <summary>A tenant id entered by hand, with no domain.</summary>
  public static EntraAcceptedTenant IdOnly(Guid tenantId) => new(tenantId, null);
}
