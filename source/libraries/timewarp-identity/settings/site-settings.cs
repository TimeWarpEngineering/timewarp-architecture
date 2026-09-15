#region Purpose
// Singleton site-settings aggregate: runtime admin policy (Entra offered/bootstrap/tenants, passkey prompt).
#endregion

#region Design
// Task 219-006: configuration owns what is *configured* (client id, secret, tenant, PublicOrigin,
// scheme registration). This aggregate owns what is *allowed* at runtime and will accumulate
// (registration, agent keys, payment toggles, banners). One row, one concurrency token — copy
// IPrincipalStore, not IAggregateRoot/Profile: Version is store-CAS via Snapshot + EntityVersion.Next,
// not AggregateDbContext. IAggregateRoot is deliberately not implemented (same rationale as
// Principal). Entity<SiteSettingsId> with a well-known Singleton id rather than a dedicated
// singleton type so the store port stays Get/Add/Update like principals.
// Defaults: EntraSignInEnabled false, EntraAllowBootstrap false, empty trusted tenants (refuse
// bootstrap), PasskeyPromptMode Soft. Empty tenants means no tenant may sync-hit or bootstrap.
// Trusted tenant ids are distinct, non-empty Guids; ReplacePolicy copies the list so callers
// cannot mutate store-resident storage. Snapshot is store-only rehydration (internal).
// Do not fold PublicOrigin / client id / secret / authority tenant into this type.
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
    IReadOnlyList<Guid> entraTrustedTenants,
    PasskeyPromptMode passkeyPromptMode,
    long version)
    : base(id, version)
  {
    EntraSignInEnabled = entraSignInEnabled;
    EntraAllowBootstrap = entraAllowBootstrap;
    EntraTrustedTenants = entraTrustedTenants;
    PasskeyPromptMode = passkeyPromptMode;
  }

  public bool EntraSignInEnabled { get; private set; }
  public bool EntraAllowBootstrap { get; private set; }
  public IReadOnlyList<Guid> EntraTrustedTenants { get; private set; }
  public PasskeyPromptMode PasskeyPromptMode { get; private set; }

  public static SiteSettings Create(
    bool entraSignInEnabled = false,
    bool entraAllowBootstrap = false,
    IReadOnlyList<Guid>? entraTrustedTenants = null,
    PasskeyPromptMode passkeyPromptMode = PasskeyPromptMode.Soft)
  {
    EnsurePasskeyPromptMode(passkeyPromptMode);
    return new SiteSettings(
      SingletonId,
      entraSignInEnabled,
      entraAllowBootstrap,
      NormalizeTenants(entraTrustedTenants),
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
      NormalizeTenants(EntraTrustedTenants),
      PasskeyPromptMode,
      version);

  public void ReplacePolicy(
    bool entraSignInEnabled,
    bool entraAllowBootstrap,
    IReadOnlyList<Guid> entraTrustedTenants,
    PasskeyPromptMode passkeyPromptMode)
  {
    EnsurePasskeyPromptMode(passkeyPromptMode);
    EntraSignInEnabled = entraSignInEnabled;
    EntraAllowBootstrap = entraAllowBootstrap;
    EntraTrustedTenants = NormalizeTenants(entraTrustedTenants);
    PasskeyPromptMode = passkeyPromptMode;
  }

  public bool IsTrustedTenant(Guid tenantId)
  {
    if (tenantId == Guid.Empty)
    {
      return false;
    }

    foreach (Guid trusted in EntraTrustedTenants)
    {
      if (trusted == tenantId)
      {
        return true;
      }
    }

    return false;
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

  private static List<Guid> NormalizeTenants(IReadOnlyList<Guid>? tenants)
  {
    if (tenants is null || tenants.Count == 0)
    {
      return [];
    }

    HashSet<Guid> seen = [];
    List<Guid> copy = [];
    foreach (Guid tenantId in tenants)
    {
      if (tenantId == Guid.Empty)
      {
        throw new ArgumentException("Trusted tenant ids must be non-empty GUIDs.", nameof(tenants));
      }

      if (seen.Add(tenantId))
      {
        copy.Add(tenantId);
      }
    }

    return copy;
  }
}
