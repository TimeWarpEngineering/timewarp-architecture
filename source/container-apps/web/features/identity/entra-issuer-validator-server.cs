#region Purpose
// OIDC TokenValidationParameters.IssuerValidator that pins iss to EntraIssuerMaterial for the token tid.
#endregion

#region Design
// Entra multi-tenant authorities (organizations/common/consumers) advertise a literal
// https://login.microsoftonline.com/{tenantid}/v2.0 issuer in metadata. Default ValidateIssuer
// rejects a concrete id_token iss; this validator substitutes tid from the token and accepts only
// iss == UTF-8 EntraIssuerMaterial.FromTenantId(tid) (ordinal) — the same pin as
// EntraTicketProcessor.IssuerMatchesTenant. Do not set ValidateIssuer=false. ASP.NET Core 10 OIDC
// uses JsonWebTokenHandler; JwtSecurityToken is retained for older token shapes. No
// Microsoft.Identity.Web (would steal DefaultScheme).
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TimeWarp.Identity;

public static class EntraIssuerValidator
{
  private const string TenantIdClaim = "tid";

  public static string Validate(string issuer, SecurityToken securityToken, TokenValidationParameters parameters)
  {
    ArgumentNullException.ThrowIfNull(securityToken);
    ArgumentNullException.ThrowIfNull(parameters);

    if (string.IsNullOrWhiteSpace(issuer))
    {
      throw new SecurityTokenInvalidIssuerException("Entra id_token issuer is missing.");
    }

    if (!TryReadTenantId(securityToken, out Guid tenantId))
    {
      throw new SecurityTokenInvalidIssuerException("Entra id_token tid claim is missing or unparsable.");
    }

    string expectedIssuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(tenantId));
    if (!string.Equals(issuer, expectedIssuer, StringComparison.Ordinal))
    {
      throw new SecurityTokenInvalidIssuerException(
        $"Entra id_token issuer '{issuer}' does not match tid '{tenantId:D}'.");
    }

    return issuer;
  }

  private static bool TryReadTenantId(SecurityToken securityToken, out Guid tenantId)
  {
    tenantId = default;

    if (securityToken is JsonWebToken jsonWebToken)
    {
      if (jsonWebToken.TryGetPayloadValue(TenantIdClaim, out string? tidText)
        && Guid.TryParse(tidText, out tenantId))
      {
        return true;
      }

      return false;
    }

    if (securityToken is JwtSecurityToken jwtSecurityToken)
    {
      string? tidText = jwtSecurityToken.Claims.FirstOrDefault(claim => claim.Type == TenantIdClaim)?.Value;
      return Guid.TryParse(tidText, out tenantId);
    }

    return false;
  }
}
