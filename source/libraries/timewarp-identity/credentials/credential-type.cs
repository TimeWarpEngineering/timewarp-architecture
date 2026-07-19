#region Purpose
// Kind of authentication material bound to a principal (WebAuthn passkey vs agent public key).
#endregion

#region Design
// Reserved zero (None) so default/missing enum values fail closed at Credential.Create rather than becoming Passkey.
#endregion

namespace TimeWarp.Identity;

public enum CredentialType
{
  None = 0,
  Passkey = 1,
  AgentKey = 2,
}
