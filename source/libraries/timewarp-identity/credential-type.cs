#region Purpose
// Kind of authentication material bound to a principal (WebAuthn passkey vs agent public key).
#endregion

namespace TimeWarp.Identity;

public enum CredentialType
{
  Passkey = 0,
  AgentKey = 1,
}
