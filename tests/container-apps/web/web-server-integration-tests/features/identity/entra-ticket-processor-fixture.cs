#region Purpose
// Shared arrange helpers for the EntraTicketProcessor test classes: trusted tenant, pinned clock origin,
// claims and credential builders, and a processor factory over in-memory stores.
#endregion

namespace EntraTicketProcessor_;

using Microsoft.Extensions.Logging.Abstractions;
using System.Text;
using TimeWarp.Architecture.Features.Identity.Application;
using TimeWarp.Identity;

internal static class EntraTicketProcessorFixture
{
  public static readonly Guid TrustedTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
  public static readonly DateTimeOffset T0 = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

  public static EntraIdTokenClaims Claims(Guid objectId, string? accountHint) =>
    new(
      TrustedTenantId,
      objectId,
      Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(TrustedTenantId)),
      "Steven Cramer",
      accountHint);

  public static async Task<byte[]> AddEntraCredentialAsync(
    InMemoryPrincipalStore principalStore,
    PrincipalId principalId,
    Guid objectId,
    string? accountHint)
  {
    byte[] handle = EntraAccountHandle.Encode(TrustedTenantId, objectId);
    await principalStore.AddCredentialAsync(
      Credential.Create(
        principalId,
        CredentialType.EntraAccount,
        handle,
        EntraIssuerMaterial.FromTenantId(TrustedTenantId),
        EntraIdTokenClaims.ProviderLabel,
        accountHint: accountHint));
    return handle;
  }

  public static async Task<EntraTicketProcessor> ProcessorAsync(
    IPrincipalStore? principalStore = null,
    TimeProvider? clock = null)
  {
    InMemorySiteSettingsStore settingsStore = new();
    await settingsStore.AddAsync(
      SiteSettings.Create(
        entraSignInEnabled: true,
        entraAllowBootstrap: true,
        passkeyPromptMode: PasskeyPromptMode.Soft,
        entraAcceptedTenants: [EntraAcceptedTenant.IdOnly(TrustedTenantId)]));
    return new EntraTicketProcessor(
      principalStore ?? new InMemoryPrincipalStore(),
      new NoOpPrincipalRoleStore(),
      new SiteSettingsEntraSignInPolicy(settingsStore),
      new CredentialUsageRecorder(clock ?? new ManualClock(T0)),
      NullLogger<EntraTicketProcessor>.Instance);
  }
}
