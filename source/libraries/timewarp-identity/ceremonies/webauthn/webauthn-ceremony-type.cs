#region Purpose
// Discriminates which WebAuthn ceremony a challenge was issued for, so a challenge minted for one
// ceremony can never be consumed by another.
#endregion

#region Design
// Reserved zero (None) so a default/uninitialized value fails closed rather than matching a
// real ceremony — mirrors PrincipalKind/CredentialType/TrustTier's reserved-zero convention.
// Merge is the add-existing-passkey assertion so a login Authentication challenge cannot
// complete a merge, and a Merge challenge cannot complete login.
#endregion

namespace TimeWarp.Identity;

public enum WebAuthnCeremonyType
{
  None = 0,
  Registration = 1,
  Authentication = 2,
  Merge = 3,
}
