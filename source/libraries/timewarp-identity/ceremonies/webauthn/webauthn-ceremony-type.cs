#region Purpose
// Discriminates which WebAuthn ceremony a challenge was issued for, so a challenge minted for one
// ceremony can never be consumed by the other.
#endregion

#region Design
// Reserved zero (None) so a default/uninitialized value fails closed rather than matching either
// real ceremony — mirrors PrincipalKind/CredentialType/TrustTier's reserved-zero convention.
#endregion

namespace TimeWarp.Identity;

public enum WebAuthnCeremonyType
{
  None = 0,
  Registration = 1,
  Authentication = 2,
}
