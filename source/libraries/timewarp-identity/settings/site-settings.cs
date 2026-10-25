#region Purpose
// Singleton site-settings aggregate: runtime admin policy (Entra offered/bootstrap, tenant
// acceptance, passkey prompt) and the assistant's display name.
#endregion

#region Design
// Task 219-006: configuration owns what is *configured* (client id, secret, authority tenant,
// PublicOrigin, scheme registration). This aggregate owns what is *allowed* at runtime and will
// accumulate (registration, agent keys, payment toggles, banners). One row, one concurrency token —
// copy IPrincipalStore, not IAggregateRoot/Profile: Version is store-CAS via Snapshot +
// EntityVersion.Next, not AggregateDbContext. IAggregateRoot is deliberately not implemented (same
// rationale as Principal). Entity<SiteSettingsId> with a well-known Singleton id rather than a
// dedicated singleton type so the store port stays Get/Add/Update like principals.
// Defaults: EntraSignInEnabled false, EntraAllowBootstrap false, PasskeyPromptMode Soft,
// AssistantName Akili (task 361), tenant acceptance Restricted with an empty list.
// AssistantName is stored trimmed; AssistantNameRules owns its rules so the contract validator
// agrees. RenameAssistant is separate from ReplacePolicy because the seeder's reseed rewrites
// offer/bootstrap from configuration and must keep the admin-set name and tenant acceptance.
// Task 306: Entra tenant acceptance is this row (Restricted list or Any), not
// Authentication:Entra:TenantId. That config value remains the OIDC authority segment.
// Task 401: each accepted tenant is an id plus an optional domain. AcceptsEntraTenant still
// matches the token tid only. EntraTenantAcceptanceInitialized is false only on rows migrated
// before the column existed; the seeder copies a GUID authority into the restricted list once,
// then leaves admin edits alone.
// Task 399: XaiByokMode is Allow unless an admin changes it. Reseed does not rewrite it.
// The zero value is Allow so a writer that omits the field does not turn personal keys off.
// Do not fold PublicOrigin / client id / secret / authority tenant into this type.
#endregion

namespace TimeWarp.Identity;

/// <summary>
/// Singleton runtime admin policy for Entra offer/bootstrap, accepted tenants, the post-Entra
/// passkey prompt, the assistant's display name, and whose xAI key Ask uses below the host key.
/// </summary>
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
    string assistantName,
    EntraTenantAcceptanceMode entraTenantAcceptanceMode,
    IReadOnlyList<EntraAcceptedTenant> entraAcceptedTenants,
    bool entraTenantAcceptanceInitialized,
    XaiByokMode xaiByokMode,
    long version)
    : base(id, version)
  {
    EntraSignInEnabled = entraSignInEnabled;
    EntraAllowBootstrap = entraAllowBootstrap;
    PasskeyPromptMode = passkeyPromptMode;
    AssistantName = assistantName;
    EntraTenantAcceptanceMode = entraTenantAcceptanceMode;
    EntraAcceptedTenants = entraAcceptedTenants;
    EntraTenantAcceptanceInitialized = entraTenantAcceptanceInitialized;
    XaiByokMode = xaiByokMode;
  }

  /// <summary>Whether Entra sign-in is offered at runtime (separate from configured client secrets).</summary>
  public bool EntraSignInEnabled { get; private set; }

  /// <summary>Whether first-time Entra users may bootstrap a new principal.</summary>
  public bool EntraAllowBootstrap { get; private set; }

  /// <summary>Soft versus required post-Entra passkey prompt policy.</summary>
  public PasskeyPromptMode PasskeyPromptMode { get; private set; }

  /// <summary>The assistant's display name (trimmed, 1-32 plain-text characters; default Akili).</summary>
  public string AssistantName { get; private set; }

  /// <summary>Restricted list or any Entra tenant. Default Restricted.</summary>
  public EntraTenantAcceptanceMode EntraTenantAcceptanceMode { get; private set; }

  /// <summary>
  /// Tenants accepted when <see cref="EntraTenantAcceptanceMode"/> is Restricted.
  /// Kept when the mode is Any so switching back does not drop the list.
  /// </summary>
  public IReadOnlyList<EntraAcceptedTenant> EntraAcceptedTenants { get; private set; }

  /// <summary>
  /// False on a row that has not yet received a tenant-acceptance value.
  /// The seeder fills that row once from a GUID <c>Authentication:Entra:TenantId</c>.
  /// </summary>
  public bool EntraTenantAcceptanceInitialized { get; private set; }

  /// <summary>Whose xAI API key Ask uses when the host key is unset. Default Allow.</summary>
  public XaiByokMode XaiByokMode { get; private set; }

  /// <summary>
  /// Creates the singleton row at version 0 with the given policy (defaults: Entra off, Soft prompt,
  /// assistant Akili, restricted tenant list empty and already initialized).
  /// </summary>
  public static SiteSettings Create(
    bool entraSignInEnabled = false,
    bool entraAllowBootstrap = false,
    PasskeyPromptMode passkeyPromptMode = PasskeyPromptMode.Soft,
    string assistantName = AssistantNameRules.Default,
    EntraTenantAcceptanceMode entraTenantAcceptanceMode = EntraTenantAcceptanceMode.Restricted,
    IEnumerable<EntraAcceptedTenant>? entraAcceptedTenants = null,
    bool entraTenantAcceptanceInitialized = true,
    XaiByokMode xaiByokMode = XaiByokMode.Allow)
  {
    EnsurePasskeyPromptMode(passkeyPromptMode);
    EnsureTenantAcceptanceMode(entraTenantAcceptanceMode);
    EnsureXaiByokMode(xaiByokMode);
    return new SiteSettings(
      SingletonId,
      entraSignInEnabled,
      entraAllowBootstrap,
      passkeyPromptMode,
      NormalizeAssistantName(assistantName),
      entraTenantAcceptanceMode,
      NormalizeAcceptedTenants(entraAcceptedTenants),
      entraTenantAcceptanceInitialized,
      xaiByokMode,
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
      AssistantName,
      EntraTenantAcceptanceMode,
      EntraAcceptedTenants.ToArray(),
      EntraTenantAcceptanceInitialized,
      XaiByokMode,
      version);

  /// <summary>
  /// Replaces offer, bootstrap, and passkey prompt. Tenant acceptance and the assistant name stay.
  /// </summary>
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

  /// <summary>
  /// Replaces tenant acceptance and marks it initialized.
  /// Restricted with an empty list accepts no tenant. Any ignores the list for decisions and keeps it.
  /// </summary>
  public void ReplaceEntraTenantAcceptance(
    EntraTenantAcceptanceMode mode,
    IEnumerable<EntraAcceptedTenant>? acceptedTenants)
  {
    EnsureTenantAcceptanceMode(mode);
    EntraTenantAcceptanceMode = mode;
    EntraAcceptedTenants = NormalizeAcceptedTenants(acceptedTenants);
    EntraTenantAcceptanceInitialized = true;
  }

  /// <summary>
  /// True when <paramref name="tenantId"/> may sign in under the stored acceptance policy.
  /// <see cref="Guid.Empty"/> is never accepted. The optional domain is not part of the match.
  /// </summary>
  public bool AcceptsEntraTenant(Guid tenantId)
  {
    if (tenantId == Guid.Empty)
    {
      return false;
    }

    if (EntraTenantAcceptanceMode == EntraTenantAcceptanceMode.Any)
    {
      return true;
    }

    if (EntraTenantAcceptanceMode != EntraTenantAcceptanceMode.Restricted)
    {
      return false;
    }

    foreach (EntraAcceptedTenant tenant in EntraAcceptedTenants)
    {
      if (tenant.TenantId == tenantId)
      {
        return true;
      }
    }

    return false;
  }

  /// <summary>Renames the assistant; the name is trimmed and must pass <see cref="AssistantNameRules.IsValid"/>.</summary>
  public void RenameAssistant(string assistantName) =>
    AssistantName = NormalizeAssistantName(assistantName);

  /// <summary>Sets whose key Ask uses below the host key.</summary>
  public void SetXaiByokMode(XaiByokMode xaiByokMode)
  {
    EnsureXaiByokMode(xaiByokMode);
    XaiByokMode = xaiByokMode;
  }

  private static string NormalizeAssistantName(string assistantName)
  {
    ArgumentNullException.ThrowIfNull(assistantName);
    if (!AssistantNameRules.IsValid(assistantName))
    {
      throw new ArgumentException(
        $"AssistantName must be 1-{AssistantNameRules.MaxLength} plain-text characters.",
        nameof(assistantName));
    }

    return assistantName.Trim();
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

  private static void EnsureXaiByokMode(XaiByokMode xaiByokMode)
  {
    if (!Enum.IsDefined(xaiByokMode))
    {
      throw new ArgumentOutOfRangeException(
        nameof(xaiByokMode),
        xaiByokMode,
        "XaiByokMode must be Allow, Required, or Off.");
    }
  }

  private static void EnsureTenantAcceptanceMode(EntraTenantAcceptanceMode mode)
  {
    if (!Enum.IsDefined(mode))
    {
      throw new ArgumentOutOfRangeException(
        nameof(mode),
        mode,
        "EntraTenantAcceptanceMode must be Restricted or Any.");
    }
  }

  private static IReadOnlyList<EntraAcceptedTenant> NormalizeAcceptedTenants(
    IEnumerable<EntraAcceptedTenant>? tenants)
  {
    if (!EntraTenantIds.TryNormalize(tenants, out IReadOnlyList<EntraAcceptedTenant> normalized, out string? error))
    {
      throw new ArgumentException(error, nameof(tenants));
    }

    return normalized;
  }
}
