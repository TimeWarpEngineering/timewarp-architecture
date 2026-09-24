#region Purpose
// The WebAuthn user name every passkey of an account carries — "TimeWarp account · <account fingerprint>".
#endregion

#region Design
// Task 253: one definition shared by the server (StartPasskeyRegistration puts it in user.name and
// user.displayName) and the SPA (Settings shows "Signed in · " + the same text), so the line in
// Settings always matches the password-manager entry. Lives in contracts because both sides
// reference them. The fingerprint is PrincipalFingerprint (per account, identical on every passkey
// of one principal) — never the per-credential CredentialFingerprint.
// Passkeys created before task 253 keep "TimeWarp user": an authenticator never lets the relying
// party change user.name after creation.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

public static class PasskeyAccountName
{
  public const string Prefix = "TimeWarp account · ";

  /// <summary>The account name for an already-computed account fingerprint.</summary>
  public static string Format(string accountFingerprint) =>
    Prefix + Guard.Against.NullOrWhiteSpace(accountFingerprint);

  /// <summary>The account name for a principal.</summary>
  public static string For(PrincipalId principalId) => Format(PrincipalFingerprint.Compute(principalId));
}
