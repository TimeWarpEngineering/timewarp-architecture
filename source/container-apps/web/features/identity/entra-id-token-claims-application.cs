#region Purpose
// Reads tid/oid/iss (and optional display name) from an Entra ID token ClaimsPrincipal.
#endregion

#region Design
// Join key is tid+oid, never email/UPN/sub (RFC 219 fork 1). MapInboundClaims is false on the
// named OIDC scheme so JWT names tid/oid/iss are used; schema URI fallbacks cover a mapped token
// if a host re-enables inbound mapping. Issuer pin is checked later against EntraIssuerMaterial.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using System.Security.Claims;

public readonly record struct EntraIdTokenClaims(Guid TenantId, Guid ObjectId, string Issuer, string? DisplayName)
{
  private const string TenantIdClaim = "tid";
  private const string ObjectIdClaim = "oid";
  private const string IssuerClaim = "iss";
  private const string TenantIdSchemaClaim = "http://schemas.microsoft.com/identity/claims/tenantid";
  private const string ObjectIdSchemaClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";

  public static bool TryRead(ClaimsPrincipal principal, out EntraIdTokenClaims claims)
  {
    ArgumentNullException.ThrowIfNull(principal);
    claims = default;

    string? tenantText = FirstClaim(principal, TenantIdClaim, TenantIdSchemaClaim);
    string? objectText = FirstClaim(principal, ObjectIdClaim, ObjectIdSchemaClaim);
    string? issuer = FirstClaim(principal, IssuerClaim);
    if (!Guid.TryParse(tenantText, out Guid tenantId)
      || !Guid.TryParse(objectText, out Guid objectId)
      || string.IsNullOrWhiteSpace(issuer))
    {
      return false;
    }

    string? displayName = FirstClaim(principal, "name") ?? FirstClaim(principal, "preferred_username");
    claims = new EntraIdTokenClaims(tenantId, objectId, issuer.Trim(), displayName);
    return true;
  }

  private static string? FirstClaim(ClaimsPrincipal principal, string claimType, string? alternateClaimType = null)
  {
    string? value = principal.FindFirst(claimType)?.Value;
    if (!string.IsNullOrWhiteSpace(value))
    {
      return value;
    }

    return alternateClaimType is null ? null : principal.FindFirst(alternateClaimType)?.Value;
  }
}
