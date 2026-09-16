#region Purpose
// Host-free coverage for EntraTicketProcessor bootstrap unique-handle race recovery.
#endregion

#region Design
// Fake IPrincipalStore: first Find misses, AddPrincipal succeeds, AddCredential throws
// InvalidOperationException, re-Find returns the winner credential — processor must sync-hit the
// winner PrincipalId (not 409). Losing AddPrincipal row is abandoned (no delete-principal port).
#endregion

namespace EntraTicketProcessor_;

using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Identity.Application;
using TimeWarp.Foundation.Types;
using TimeWarp.Identity;

public class Bootstrap_Given_
{
  private static readonly Guid TrustedTenantId = Guid.Parse("30f3971f-4719-4f20-9b6f-88916e0b95bd");

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
        passkeyPromptMode: PasskeyPromptMode.Soft));
    EntraTicketProcessor processor = new(
      principalStore,
      new NoOpPrincipalRoleStore(),
      new SiteSettingsEntraSignInPolicy(settingsStore, ConfiguredTenant()),
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
        passkeyPromptMode: PasskeyPromptMode.Soft));
    EntraTicketProcessor processor = new(
      new InMemoryPrincipalStore(),
      new NoOpPrincipalRoleStore(),
      new SiteSettingsEntraSignInPolicy(settingsStore, ConfiguredTenant()),
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

  public static async Task Link_Foreign_Active_Handle_Should_Merge()
  {
    InMemoryPrincipalStore principalStore = new();
    Principal owner = Principal.Create(PrincipalKind.Human);
    owner.SetDisplayName("Owner");
    await principalStore.AddPrincipalAsync(owner);
    Guid objectId = Guid.NewGuid();
    await principalStore.AddCredentialAsync(
      Credential.Create(
        owner.Id,
        CredentialType.EntraAccount,
        EntraAccountHandle.Encode(TrustedTenantId, objectId),
        EntraIssuerMaterial.FromTenantId(TrustedTenantId),
        "Microsoft 365"));
    Principal caller = Principal.Create(PrincipalKind.Human);
    caller.SetDisplayName("Caller");
    await principalStore.AddPrincipalAsync(caller);
    EntraTicketProcessor processor = await ProcessorAsync(principalStore);
    string issuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(TrustedTenantId));
    EntraIdTokenClaims claims = new(TrustedTenantId, objectId, issuer, "Owner");

    OneOf<PrincipalId, EntraChoiceRequired, SharedProblemDetails> result = await processor.ProcessAsync(
      claims,
      EntraTicketProcessor.ModeLink,
      caller.Id,
      CancellationToken.None);

    result.IsT0.ShouldBeTrue();
    result.AsT0.ShouldBe(caller.Id);
    Principal? retired = await principalStore.GetPrincipalAsync(owner.Id);
    retired.ShouldNotBeNull();
    retired.IsActive.ShouldBeFalse();
    retired.MergedIntoPrincipalId.ShouldBe(caller.Id);
  }

  public static async Task Link_Foreign_Revoked_Handle_Should_Refuse_Without_Merge()
  {
    InMemoryPrincipalStore principalStore = new();
    Principal owner = Principal.Create(PrincipalKind.Human);
    owner.SetDisplayName("Owner");
    await principalStore.AddPrincipalAsync(owner);
    Guid objectId = Guid.NewGuid();
    Credential entraCredential = Credential.Create(
      owner.Id,
      CredentialType.EntraAccount,
      EntraAccountHandle.Encode(TrustedTenantId, objectId),
      EntraIssuerMaterial.FromTenantId(TrustedTenantId),
      "Microsoft 365");
    await principalStore.AddCredentialAsync(entraCredential);
    Credential? stored = await principalStore.GetCredentialAsync(entraCredential.Id);
    stored.ShouldNotBeNull();
    stored.Revoke();
    await principalStore.UpdateCredentialAsync(stored);
    Principal caller = Principal.Create(PrincipalKind.Human);
    caller.SetDisplayName("Caller");
    await principalStore.AddPrincipalAsync(caller);
    EntraTicketProcessor processor = await ProcessorAsync(principalStore);
    string issuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(TrustedTenantId));
    EntraIdTokenClaims claims = new(TrustedTenantId, objectId, issuer, "Owner");

    OneOf<PrincipalId, EntraChoiceRequired, SharedProblemDetails> result = await processor.ProcessAsync(
      claims,
      EntraTicketProcessor.ModeLink,
      caller.Id,
      CancellationToken.None);

    result.IsT2.ShouldBeTrue();
    result.AsT2.Status.ShouldBe(403);
    result.AsT2.Title.ShouldBe("Entra credential revoked");
    Principal? ownerAfter = await principalStore.GetPrincipalAsync(owner.Id);
    ownerAfter.ShouldNotBeNull();
    ownerAfter.IsActive.ShouldBeTrue();
    ownerAfter.MergedIntoPrincipalId.ShouldBeNull();
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

  public static async Task Foreign_Tid_On_Link_Should_Refuse()
  {
    InMemoryPrincipalStore principalStore = new();
    Principal caller = Principal.Create(PrincipalKind.Human);
    await principalStore.AddPrincipalAsync(caller);
    EntraTicketProcessor processor = await ProcessorAsync(principalStore);
    Guid foreignTenant = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    string issuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(foreignTenant));
    EntraIdTokenClaims claims = new(foreignTenant, Guid.NewGuid(), issuer, "Foreign");

    OneOf<PrincipalId, EntraChoiceRequired, SharedProblemDetails> result = await processor.ProcessAsync(
      claims,
      EntraTicketProcessor.ModeLink,
      caller.Id,
      CancellationToken.None);

    result.IsT2.ShouldBeTrue();
    result.AsT2.Title.ShouldBe("Untrusted tenant");
    result.AsT2.Status.ShouldBe(403);
  }

  private static IOptions<EntraAuthenticationOptions> ConfiguredTenant() =>
    Options.Create(new EntraAuthenticationOptions { TenantId = TrustedTenantId.ToString("D") });

  public static async Task Bootstrap_Should_Set_Label_From_Preferred_Username()
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
    stored!.Label.ShouldBe("Steven.Cramer@TimeWarp.Enterprises");
  }

  public static async Task Bootstrap_Should_Set_Label_From_Name_When_Preferred_Username_Missing()
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
    stored!.Label.ShouldBe("Steven Cramer");
  }

  public static async Task Link_Second_Active_Entra_Should_409_Already_Linked()
  {
    InMemoryPrincipalStore principalStore = new();
    Principal caller = Principal.Create(PrincipalKind.Human);
    await principalStore.AddPrincipalAsync(caller);
    Guid firstObjectId = Guid.NewGuid();
    await principalStore.AddCredentialAsync(
      Credential.Create(
        caller.Id,
        CredentialType.EntraAccount,
        EntraAccountHandle.Encode(TrustedTenantId, firstObjectId),
        EntraIssuerMaterial.FromTenantId(TrustedTenantId),
        "Steven.Cramer@TimeWarp.Enterprises"));
    EntraTicketProcessor processor = await ProcessorAsync(principalStore);
    string issuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(TrustedTenantId));
    EntraIdTokenClaims claims = new(
      TrustedTenantId,
      Guid.NewGuid(),
      issuer,
      "Other User",
      "other@TimeWarp.Enterprises");

    OneOf<PrincipalId, EntraChoiceRequired, SharedProblemDetails> result = await processor.ProcessAsync(
      claims,
      EntraTicketProcessor.ModeLink,
      caller.Id,
      CancellationToken.None);

    result.IsT2.ShouldBeTrue();
    result.AsT2.Title.ShouldBe("Microsoft 365 already linked");
    result.AsT2.Status.ShouldBe(409);
    IReadOnlyList<Credential> credentials = await principalStore.ListCredentialsAsync(caller.Id);
    credentials.Count(c => c.Type == CredentialType.EntraAccount && !c.IsRevoked).ShouldBe(1);
  }

  public static async Task Link_Should_Set_Label_From_Preferred_Username()
  {
    InMemoryPrincipalStore principalStore = new();
    Principal caller = Principal.Create(PrincipalKind.Human);
    await principalStore.AddPrincipalAsync(caller);
    EntraTicketProcessor processor = await ProcessorAsync(principalStore);
    Guid objectId = Guid.NewGuid();
    string issuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(TrustedTenantId));
    EntraIdTokenClaims claims = new(
      TrustedTenantId,
      objectId,
      issuer,
      "Steven Cramer",
      "Steven.Cramer@TimeWarp.Enterprises");

    OneOf<PrincipalId, EntraChoiceRequired, SharedProblemDetails> result = await processor.ProcessAsync(
      claims,
      EntraTicketProcessor.ModeLink,
      caller.Id,
      CancellationToken.None);

    result.IsT0.ShouldBeTrue();
    Credential? stored = await principalStore.FindCredentialByHandleAsync(
      CredentialType.EntraAccount,
      EntraAccountHandle.Encode(TrustedTenantId, objectId));
    stored.ShouldNotBeNull();
    stored!.Label.ShouldBe("Steven.Cramer@TimeWarp.Enterprises");
  }

  public static async Task Link_Same_Handle_Again_Should_409_Already_On_This_Account()
  {
    InMemoryPrincipalStore principalStore = new();
    Principal caller = Principal.Create(PrincipalKind.Human);
    await principalStore.AddPrincipalAsync(caller);
    Guid objectId = Guid.NewGuid();
    await principalStore.AddCredentialAsync(
      Credential.Create(
        caller.Id,
        CredentialType.EntraAccount,
        EntraAccountHandle.Encode(TrustedTenantId, objectId),
        EntraIssuerMaterial.FromTenantId(TrustedTenantId),
        "Steven.Cramer@TimeWarp.Enterprises"));
    EntraTicketProcessor processor = await ProcessorAsync(principalStore);
    string issuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(TrustedTenantId));
    EntraIdTokenClaims claims = new(TrustedTenantId, objectId, issuer, "Steven Cramer");

    OneOf<PrincipalId, EntraChoiceRequired, SharedProblemDetails> result = await processor.ProcessAsync(
      claims,
      EntraTicketProcessor.ModeLink,
      caller.Id,
      CancellationToken.None);

    result.IsT2.ShouldBeTrue();
    result.AsT2.Title.ShouldBe("Already on this account");
    result.AsT2.Status.ShouldBe(409);
  }

  public static async Task Bootstrap_Should_Fall_Back_Label_When_Claims_Have_No_Name()
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
    stored!.Label.ShouldBe(EntraIdTokenClaims.FallbackCredentialLabel);
  }

  private static async Task<EntraTicketProcessor> ProcessorAsync(IPrincipalStore? principalStore = null)
  {
    InMemorySiteSettingsStore settingsStore = new();
    await settingsStore.AddAsync(
      SiteSettings.Create(
        entraSignInEnabled: true,
        entraAllowBootstrap: true,
        passkeyPromptMode: PasskeyPromptMode.Soft));
    return new EntraTicketProcessor(
      principalStore ?? new InMemoryPrincipalStore(),
      new NoOpPrincipalRoleStore(),
      new SiteSettingsEntraSignInPolicy(settingsStore, ConfiguredTenant()),
      NullLogger<EntraTicketProcessor>.Instance);
  }

  private sealed class NoOpPrincipalRoleStore : IPrincipalRoleStore
  {
    public Task<IReadOnlyList<Guid>> GetRoleIdsAsync(
      PrincipalId principalId,
      CancellationToken cancellationToken = default) =>
      Task.FromResult<IReadOnlyList<Guid>>([]);

    public Task SetRoleIdsAsync(
      PrincipalId principalId,
      IReadOnlyList<Guid> roleIds,
      CancellationToken cancellationToken = default) =>
      Task.CompletedTask;

    public Task<bool> TryClaimFirstAdministratorAsync(
      PrincipalId principalId,
      CancellationToken cancellationToken = default) =>
      Task.FromResult(false);
  }

  private sealed class RacePrincipalStore : IPrincipalStore
  {
    private readonly Principal WinnerPrincipal;
    private readonly Credential WinnerCredential;
    private int FindCredentialCallCount;

    public RacePrincipalStore(Principal winnerPrincipal, Credential winnerCredential)
    {
      WinnerPrincipal = winnerPrincipal;
      WinnerCredential = winnerCredential;
    }

    public int FindCredentialCalls => FindCredentialCallCount;
    public int AddPrincipalCalls { get; private set; }
    public int AddCredentialCalls { get; private set; }

    public Task AddPrincipalAsync(Principal principal, CancellationToken cancellationToken = default)
    {
      AddPrincipalCalls++;
      return Task.CompletedTask;
    }

    public Task<Principal?> GetPrincipalAsync(PrincipalId id, CancellationToken cancellationToken = default)
    {
      if (id == WinnerPrincipal.Id)
      {
        return Task.FromResult<Principal?>(WinnerPrincipal);
      }

      return Task.FromResult<Principal?>(null);
    }

    public Task UpdatePrincipalAsync(Principal principal, CancellationToken cancellationToken = default) =>
      Task.CompletedTask;

    public Task<IReadOnlyList<Principal>> ListPrincipalsAsync(CancellationToken cancellationToken = default) =>
      Task.FromResult<IReadOnlyList<Principal>>([WinnerPrincipal]);

    public Task AddCredentialAsync(Credential credential, CancellationToken cancellationToken = default)
    {
      AddCredentialCalls++;
      throw new InvalidOperationException(
        "A credential with type EntraAccount and this handle already exists.");
    }

    public Task<Credential?> GetCredentialAsync(
      CredentialId credentialId,
      CancellationToken cancellationToken = default) =>
      Task.FromResult<Credential?>(null);

    public Task<Credential?> FindCredentialByHandleAsync(
      CredentialType type,
      byte[] handle,
      CancellationToken cancellationToken = default)
    {
      FindCredentialCallCount++;
      if (FindCredentialCallCount == 1)
      {
        return Task.FromResult<Credential?>(null);
      }

      return Task.FromResult<Credential?>(WinnerCredential);
    }

    public Task<IReadOnlyList<Credential>> ListCredentialsAsync(
      PrincipalId principalId,
      bool includeRevoked = false,
      CancellationToken cancellationToken = default) =>
      Task.FromResult<IReadOnlyList<Credential>>([]);

    public Task UpdateCredentialAsync(Credential credential, CancellationToken cancellationToken = default) =>
      Task.CompletedTask;

    public Task MergePrincipalAsync(
      PrincipalId sourceId,
      PrincipalId targetId,
      CancellationToken cancellationToken = default) =>
      Task.CompletedTask;
  }
}
