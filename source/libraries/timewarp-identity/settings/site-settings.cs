#region Purpose
// Singleton site-settings aggregate: runtime admin policy (Entra offered/bootstrap, passkey prompt).
#endregion

#region Design
// Task 219-006: configuration owns what is *configured* (client id, secret, tenant, PublicOrigin,
// scheme registration). This aggregate owns what is *allowed* at runtime and will accumulate
// (registration, agent keys, payment toggles, banners). One row, one concurrency token — copy
// IPrincipalStore, not IAggregateRoot/Profile: Version is store-CAS via Snapshot + EntityVersion.Next,
// not AggregateDbContext. IAggregateRoot is deliberately not implemented (same rationale as
// Principal). Entity<SiteSettingsId> with a well-known Singleton id rather than a dedicated
// singleton type so the store port stays Get/Add/Update like principals.
// Defaults: EntraSignInEnabled false, EntraAllowBootstrap false, PasskeyPromptMode Soft.
// Task 227: trust is Authentication:Entra:TenantId (token tid GUID-equals the configured tenant).
// This aggregate does not store a tenant allowlist. Do not fold PublicOrigin / client id /
// secret / authority tenant into this type.
#endregion

namespace TimeWarp.Identity;

public sealed class SiteSettings : Entity<SiteSettingsId>
{
  /// <summary>
  /// Well-known singleton id for the one site-settings row. Never minted per environment.
  /// </summary>
  public static SiteSettingsId SingletonId { get; } =
    SiteSettingsId.From(Guid.Parse("6f1c2e90-2190-4006-8000-51ee77000001"));

  private SiteSettings(
    SiteSettingsId id,
    bool entraSignInEnabled,
    bool entraAllowBootstrap,
    PasskeyPromptMode passkeyPromptMode,
    long version)
    : base(id, version)
  {
    EntraSignInEnabled = entraSignInEnabled;
    EntraAllowBootstrap = entraAllowBootstrap;
    PasskeyPromptMode = passkeyPromptMode;
  }

  public bool EntraSignInEnabled { get; private set; }
  public bool EntraAllowBootstrap { get; private set; }
  public PasskeyPromptMode PasskeyPromptMode { get; private set; }

  public static SiteSettings Create(
    bool entraSignInEnabled = false,
    bool entraAllowBootstrap = false,
    PasskeyPromptMode passkeyPromptMode = PasskeyPromptMode.Soft)
  {
    EnsurePasskeyPromptMode(passkeyPromptMode);
    return new SiteSettings(
      SingletonId,
      entraSignInEnabled,
      entraAllowBootstrap,
      passkeyPromptMode,
      version: 0);
  }

  /// <summary>
  /// Store-only rehydration: copies already-valid state at a specific version — no id minting.
  /// </summary>
  internal SiteSettings Snapshot(long version) =>
    new(
      Id,
      EntraSignInEnabled,
      EntraAllowBootstrap,
      PasskeyPromptMode,
      version);

  public void ReplacePolicy(
    bool entraSignInEnabled,
    bool entraAllowBootstrap,
    PasskeyPromptMode passkeyPromptMode)
  {
    EnsurePasskeyPromptMode(passkeyPromptMode);
    EntraSignInEnabled = entraSignInEnabled;
    EntraAllowBootstrap = entraAllowBootstrap;
    PasskeyPromptMode = passkeyPromptMode;
  }

  private static void EnsurePasskeyPromptMode(PasskeyPromptMode passkeyPromptMode)
  {
    if (!Enum.IsDefined(passkeyPromptMode))
    {
      throw new ArgumentOutOfRangeException(
        nameof(passkeyPromptMode),
        passkeyPromptMode,
        "PasskeyPromptMode must be Soft or Required.");
    }
  }
}
