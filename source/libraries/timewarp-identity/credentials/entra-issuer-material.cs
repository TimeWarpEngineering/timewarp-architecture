#region Purpose
// Pins the Entra v2.0 issuer URI as PublicMaterial for EntraAccount credentials.
#endregion

#region Design
// PublicMaterial is type-dependent: COSE/SPKI for Passkey/AgentKey; this issuer URI for EntraAccount
// only. Hosts must never feed these bytes into WebAuthnAuthentication.Verify or AgentKeyProof.Verify.
// Issuer is https://login.microsoftonline.com/{tid}/v2.0 with canonical lowercase tid.
// Library stays Graph-free and OIDC-free — this is the pin, not a token validator.
#endregion

namespace TimeWarp.Identity;

/// <summary>
/// Builds the Entra v2.0 issuer URI bytes stored as <see cref="Credential.PublicMaterial"/> for EntraAccount rows.
/// </summary>
public static class EntraIssuerMaterial
{
  /// <summary>
  /// UTF-8 Entra v2.0 issuer URI pinned as <see cref="Credential.PublicMaterial"/> for
  /// <see cref="CredentialType.EntraAccount"/> rows. Verification is OIDC token validation against
  /// this issuer, not a public-key verify.
  /// </summary>
  public static byte[] FromTenantId(Guid tenantId)
  {
    string issuer = $"https://login.microsoftonline.com/{tenantId:D}/v2.0";
    return Encoding.UTF8.GetBytes(issuer);
  }
}
