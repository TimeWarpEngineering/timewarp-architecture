#region Purpose
// Kind of authentication material bound to a principal (WebAuthn passkey, agent public key, or Entra account).
#endregion

#region Design
// Reserved zero (None) so default/missing enum values fail closed at Credential.Create rather than becoming Passkey.
// EntraAccount = 3 is a linked Entra (tid, oid) identity on the same Credential aggregate — not a second login table
// (RFC 219 D2 A′). Join key is Handle = UTF-8 "{tid}:{oid}" (EntraAccountHandle); never email, UPN, or OIDC sub.
// PublicMaterial for EntraAccount is issuer URI bytes (EntraIssuerMaterial), not a public key — hosts must never
// feed EntraAccount rows into WebAuthnAuthentication.Verify or AgentKeyProof.Verify.
#endregion

namespace TimeWarp.Identity;

/// <summary>
/// Kind of authentication material bound to a principal (passkey, agent key, or Entra account).
/// </summary>
public enum CredentialType
{
  /// <summary>Uninitialized value; rejected by <see cref="Credential.Create"/>.</summary>
  None = 0,

  /// <summary>WebAuthn passkey; handle is credential id, public material is COSE key bytes.</summary>
  Passkey = 1,

  /// <summary>Agent ECDSA P-256 key; handle is server-computed key id, public material is SPKI DER.</summary>
  AgentKey = 2,

  /// <summary>Linked Entra (tid, oid); handle is <c>{tid}:{oid}</c>, public material is issuer URI bytes.</summary>
  EntraAccount = 3,
}
