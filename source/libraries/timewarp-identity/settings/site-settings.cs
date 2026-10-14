#region Purpose
// Singleton site-settings aggregate: runtime admin policy (Entra offered/bootstrap, passkey prompt)
// and the assistant's display name.
#endregion

#region Design
// Task 219-006: configuration owns what is *configured* (client id, secret, tenant, PublicOrigin,
// scheme registration). This aggregate owns what is *allowed* at runtime and will accumulate
// (registration, agent keys, payment toggles, banners). One row, one concurrency token — copy
// IPrincipalStore, not IAggregateRoot/Profile: Version is store-CAS via Snapshot + EntityVersion.Next,
// not AggregateDbContext. IAggregateRoot is deliberately not implemented (same rationale as
// Principal). Entity<SiteSettingsId> with a well-known Singleton id rather than a dedicated
// singleton type so the store port stays Get/Add/Update like principals.
// Defaults: EntraSignInEnabled false, EntraAllowBootstrap false, PasskeyPromptMode Soft,
// AssistantName Akili (task 361). AssistantName is stored trimmed; AssistantNameRules owns its
// rules so the contract validator agrees. RenameAssistant is separate from ReplacePolicy because
// the seeder's reseed rewrites policy from configuration and must keep the admin-set name.
// Task 227: trust is Authentication:Entra:TenantId (token tid GUID-equals the configured tenant).
// This aggregate does not store a tenant allowlist. Do not fold PublicOrigin / client id /
// secret / authority tenant into this type.
#endregion

namespace TimeWarp.Identity;

/// <summary>
/// Singleton runtime admin policy for Entra offer/bootstrap, the post-Entra passkey prompt, and the
/// assistant's display name.
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
    long version)
    : base(id, version)
  {
    EntraSignInEnabled = entraSignInEnabled;
    EntraAllowBootstrap = entraAllowBootstrap;
    PasskeyPromptMode = passkeyPromptMode;
    AssistantName = assistantName;
  }

  /// <summary>Whether Entra sign-in is offered at runtime (separate from configured client secrets).</summary>
  public bool EntraSignInEnabled { get; private set; }

  /// <summary>Whether first-time Entra users may bootstrap a new principal.</summary>
  public bool EntraAllowBootstrap { get; private set; }

  /// <summary>Soft versus required post-Entra passkey prompt policy.</summary>
  public PasskeyPromptMode PasskeyPromptMode { get; private set; }

  /// <summary>The assistant's display name (trimmed, 1-32 plain-text characters; default Akili).</summary>
  public string AssistantName { get; private set; }

  /// <summary>
  /// Creates the singleton row at version 0 with the given policy (defaults: Entra off, Soft prompt,
  /// assistant Akili).
  /// </summary>
  public static SiteSettings Create(
    bool entraSignInEnabled = false,
    bool entraAllowBootstrap = false,
    PasskeyPromptMode passkeyPromptMode = PasskeyPromptMode.Soft,
    string assistantName = AssistantNameRules.Default)
  {
    EnsurePasskeyPromptMode(passkeyPromptMode);
    return new SiteSettings(
      SingletonId,
      entraSignInEnabled,
      entraAllowBootstrap,
      passkeyPromptMode,
      NormalizeAssistantName(assistantName),
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
      version);

  /// <summary>Replaces the whole runtime policy surface in one mutation.</summary>
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

  /// <summary>Renames the assistant; the name is trimmed and must pass <see cref="AssistantNameRules.IsValid"/>.</summary>
  public void RenameAssistant(string assistantName) =>
    AssistantName = NormalizeAssistantName(assistantName);

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
}
