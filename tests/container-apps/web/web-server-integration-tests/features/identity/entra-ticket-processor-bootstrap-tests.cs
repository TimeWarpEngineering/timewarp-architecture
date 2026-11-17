#region Purpose
// Host-free coverage for EntraTicketProcessor bootstrap: unique-handle race recovery, issuer / tenant
// refusals, choice-required, create, and the credential provider Label + preferred_username hint (task 250).
#endregion

#region Design
// Fake IPrincipalStore: first Find misses, AddPrincipal succeeds, AddCredential throws
// InvalidOperationException, re-Find returns the winner credential — processor must sync-hit the
// winner PrincipalId (not 409). Losing AddPrincipal row is abandoned (no delete-principal port).
// Last-used tests pin the recorder's clock (ManualClock) and advance it between sign-ins: a stamp that
// does not advance is not written, so the hint + stamp single-write is asserted as exactly one
// Version bump per sign-in on InMemoryPrincipalStore.
#endregion

namespace EntraTicketProcessor_;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using System.Text;
using TimeWarp.Architecture.Features.Identity.Application;
using TimeWarp.Foundation.Types;
using TimeWarp.Identity;
using static EntraTicketProcessor_.EntraTicketProcessorFixture;

public class Bootstrap_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Bootstrap_Given_>();

  public static async Task AddCredential_Race_Should_Sync_Hit_Winner_Principal()
  {
    Guid objectId = Guid.NewGuid();
    Principal winnerPrincipal = Principal.Create(PrincipalKind.Human);
    Credential winnerCredential = Credential.Create(
      winnerPrincipal.Id,
      CredentialType.EntraAccount,
      EntraAccountHandle.Encode(TrustedTenantId, objectId),
      EntraIssuerMaterial.FromTenantId(TrustedTenantId),
      "Microsoft 365");

    RacePrincipalStore principalStore = new(winnerPrincipal, winnerCredential);
    InMemorySiteSettingsStore settingsStore = new();
    await settingsStore.AddAsync(
      SiteSettings.Create(
        entraSignInEnabled: true,
        entraAllowBootstrap: true,
        passkeyPromptMode: PasskeyPromptMode.Soft,
        entraAcceptedTenants: [EntraAcceptedTenant.IdOnly(TrustedTenantId)]));
    EntraTicketProcessor processor = new(
      principalStore,
      new NoOpPrincipalRoleStore(),
      new SiteSettingsEntraSignInPolicy(settingsStore),
      new CredentialUsageRecorder(),
      NullLogger<EntraTicketProcessor>.Instance);

    string issuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(TrustedTenantId));
    EntraIdTokenClaims claims = new(TrustedTenantId, objectId, issuer, "Race Loser");

    OneOf<PrincipalId, SharedProblemDetails> result = await processor.CompleteBootstrapCreateAsync(
      claims,
      CancellationToken.None);

    result.IsT0.ShouldBeTrue("Losing AddCredentialAsync race must sync-hit the winner, not return 409.");
    result.AsT0.ShouldBe(winnerPrincipal.Id);
    principalStore.FindCredentialCalls.ShouldBe(2);
    principalStore.AddPrincipalCalls.ShouldBe(1);
    principalStore.AddCredentialCalls.ShouldBe(1);
  }

  public static async Task Issuer_Mismatch_Should_400_And_Log_Expected_Vs_Token_Issuer()
  {
    FakeLogger<EntraTicketProcessor> logger = new();
    InMemorySiteSettingsStore settingsStore = new();
    await settingsStore.AddAsync(
      SiteSettings.Create(
        entraSignInEnabled: true,
        entraAllowBootstrap: true,
        passkeyPromptMode: PasskeyPromptMode.Soft,
        entraAcceptedTenants: [EntraAcceptedTenant.IdOnly(TrustedTenantId)]));
    EntraTicketProcessor processor = new(
      new InMemoryPrincipalStore(),
      new NoOpPrincipalRoleStore(),
      new SiteSettingsEntraSignInPolicy(settingsStore),
      new CredentialUsageRecorder(),
      logger);

    string expectedIssuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(TrustedTenantId));
    const string tokenIssuer = "https://login.microsoftonline.com/wrong/v2.0";
    EntraIdTokenClaims claims = new(TrustedTenantId, Guid.NewGuid(), tokenIssuer, "Mismatch");

    OneOf<PrincipalId, EntraChoiceRequired, SharedProblemDetails> result = await processor.ProcessAsync(
      claims,
      EntraTicketProcessor.ModeBootstrap,
      linkCallerPrincipalId: null,
      CancellationToken.None);

    result.IsT2.ShouldBeTrue();
    result.AsT2.Title.ShouldBe("Invalid Entra token");
    result.AsT2.Detail.ShouldBe("The Entra ID token issuer does not match the tenant.");
    FakeLogRecord record = logger.Collector.LatestRecord;
    record.Level.ShouldBe(LogLevel.Warning);
    record.Message.ShouldContain(expectedIssuer);
    record.Message.ShouldContain(tokenIssuer);
    record.Message.ShouldNotContain(claims.ObjectId.ToString("D"));
  }

  public static async Task Foreign_Tid_On_Bootstrap_Should_Refuse()
  {
    EntraTicketProcessor processor = await ProcessorAsync();
    Guid foreignTenant = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    string issuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(foreignTenant));
    EntraIdTokenClaims claims = new(foreignTenant, Guid.NewGuid(), issuer, "Foreign");

    OneOf<PrincipalId, EntraChoiceRequired, SharedProblemDetails> result = await processor.ProcessAsync(
      claims,
      EntraTicketProcessor.ModeBootstrap,
      linkCallerPrincipalId: null,
      CancellationToken.None);

    result.IsT2.ShouldBeTrue();
    result.AsT2.Title.ShouldBe("Untrusted tenant");
    result.AsT2.Status.ShouldBe(403);
  }

  public static async Task Unknown_Handle_With_Bootstrap_Allowed_Should_Require_Choice()
  {
    EntraTicketProcessor processor = await ProcessorAsync();
    string issuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(TrustedTenantId));
    EntraIdTokenClaims claims = new(TrustedTenantId, Guid.NewGuid(), issuer, "Chooser");

    OneOf<PrincipalId, EntraChoiceRequired, SharedProblemDetails> result = await processor.ProcessAsync(
      claims,
      EntraTicketProcessor.ModeBootstrap,
      linkCallerPrincipalId: null,
      CancellationToken.None);

    result.IsT1.ShouldBeTrue();
  }

  public static async Task CompleteBootstrapCreate_Should_Mint_Principal()
  {
    InMemoryPrincipalStore store = new();
    EntraTicketProcessor processor = await ProcessorAsync(store);
    string issuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(TrustedTenantId));
    EntraIdTokenClaims claims = new(TrustedTenantId, Guid.NewGuid(), issuer, "New Human");

    OneOf<PrincipalId, SharedProblemDetails> result =
      await processor.CompleteBootstrapCreateAsync(claims, CancellationToken.None);

    result.IsT0.ShouldBeTrue();
    Principal? created = await store.GetPrincipalAsync(result.AsT0);
    created.ShouldNotBeNull();
    created.DisplayName.ShouldBe("New Human");
    created.IsActive.ShouldBeTrue();
  }

  public static async Task Bootstrap_Should_Store_Provider_Label_And_Preferred_Username_Hint()
  {
    InMemoryPrincipalStore principalStore = new();
    EntraTicketProcessor processor = await ProcessorAsync(principalStore);
    Guid objectId = Guid.NewGuid();
    string issuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(TrustedTenantId));
    EntraIdTokenClaims claims = new(
      TrustedTenantId,
      objectId,
      issuer,
      "Steven Cramer",
      "Steven.Cramer@TimeWarp.Enterprises");

    OneOf<PrincipalId, SharedProblemDetails> result =
      await processor.CompleteBootstrapCreateAsync(claims, CancellationToken.None);

    result.IsT0.ShouldBeTrue();
    Credential? stored = await principalStore.FindCredentialByHandleAsync(
      CredentialType.EntraAccount,
      EntraAccountHandle.Encode(TrustedTenantId, objectId));
    stored.ShouldNotBeNull();
    stored!.Label.ShouldBe(EntraIdTokenClaims.ProviderLabel);
    stored.AccountHint.ShouldBe("Steven.Cramer@TimeWarp.Enterprises");
  }

  public static async Task Bootstrap_Without_Preferred_Username_Should_Store_No_Hint()
  {
    InMemoryPrincipalStore principalStore = new();
    EntraTicketProcessor processor = await ProcessorAsync(principalStore);
    Guid objectId = Guid.NewGuid();
    string issuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(TrustedTenantId));
    EntraIdTokenClaims claims = new(TrustedTenantId, objectId, issuer, "Steven Cramer");

    OneOf<PrincipalId, SharedProblemDetails> result =
      await processor.CompleteBootstrapCreateAsync(claims, CancellationToken.None);

    result.IsT0.ShouldBeTrue();
    Credential? stored = await principalStore.FindCredentialByHandleAsync(
      CredentialType.EntraAccount,
      EntraAccountHandle.Encode(TrustedTenantId, objectId));
    stored.ShouldNotBeNull();
    stored!.Label.ShouldBe(EntraIdTokenClaims.ProviderLabel);
    stored.AccountHint.ShouldBeNull("the name claim names the person, not the account");
  }

  public static async Task Bootstrap_Should_Store_Provider_Label_When_Claims_Have_No_Name()
  {
    InMemoryPrincipalStore principalStore = new();
    EntraTicketProcessor processor = await ProcessorAsync(principalStore);
    Guid objectId = Guid.NewGuid();
    string issuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(TrustedTenantId));
    EntraIdTokenClaims claims = new(TrustedTenantId, objectId, issuer, DisplayName: null);

    OneOf<PrincipalId, SharedProblemDetails> result =
      await processor.CompleteBootstrapCreateAsync(claims, CancellationToken.None);

    result.IsT0.ShouldBeTrue();
    Credential? stored = await principalStore.FindCredentialByHandleAsync(
      CredentialType.EntraAccount,
      EntraAccountHandle.Encode(TrustedTenantId, objectId));
    stored.ShouldNotBeNull();
    stored!.Label.ShouldBe(EntraIdTokenClaims.ProviderLabel);
    stored.AccountHint.ShouldBeNull();
  }
}
