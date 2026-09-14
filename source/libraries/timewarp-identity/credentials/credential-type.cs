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

public enum CredentialType
{
  None = 0,
  Passkey = 1,
  AgentKey = 2,
  EntraAccount = 3,
}
