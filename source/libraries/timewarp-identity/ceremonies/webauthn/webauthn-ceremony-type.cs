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

/// <summary>
/// Ceremony a one-time WebAuthn challenge was issued for — registration, sign-in, and merge stay separated.
/// </summary>
public enum WebAuthnCeremonyType
{
  /// <summary>Uninitialized value; fails closed and matches no real ceremony.</summary>
  None = 0,

  /// <summary>Create a new passkey (<c>webauthn.create</c>).</summary>
  Registration = 1,

  /// <summary>Sign in with an existing passkey (<c>webauthn.get</c>).</summary>
  Authentication = 2,

  /// <summary>Assert an existing passkey to merge it onto another principal.</summary>
  Merge = 3,
}
