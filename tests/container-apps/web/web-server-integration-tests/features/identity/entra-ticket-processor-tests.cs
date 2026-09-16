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

    OneOf<PrincipalId, SharedProblemDetails> result = await processor.ProcessAsync(
      claims,
      EntraTicketProcessor.ModeBootstrap,
      linkCallerPrincipalId: null,
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

    OneOf<PrincipalId, SharedProblemDetails> result = await processor.ProcessAsync(
      claims,
      EntraTicketProcessor.ModeBootstrap,
      linkCallerPrincipalId: null,
      CancellationToken.None);

    result.IsT1.ShouldBeTrue();
    result.AsT1.Title.ShouldBe("Invalid Entra token");
    result.AsT1.Detail.ShouldBe("The Entra ID token issuer does not match the tenant.");
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

    OneOf<PrincipalId, SharedProblemDetails> result = await processor.ProcessAsync(
      claims,
      EntraTicketProcessor.ModeBootstrap,
      linkCallerPrincipalId: null,
      CancellationToken.None);

    result.IsT1.ShouldBeTrue();
    result.AsT1.Title.ShouldBe("Untrusted tenant");
    result.AsT1.Status.ShouldBe(403);
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

    OneOf<PrincipalId, SharedProblemDetails> result = await processor.ProcessAsync(
      claims,
      EntraTicketProcessor.ModeLink,
      caller.Id,
      CancellationToken.None);

    result.IsT1.ShouldBeTrue();
    result.AsT1.Title.ShouldBe("Untrusted tenant");
    result.AsT1.Status.ShouldBe(403);
  }

  private static IOptions<EntraAuthenticationOptions> ConfiguredTenant() =>
    Options.Create(new EntraAuthenticationOptions { TenantId = TrustedTenantId.ToString("D") });

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
  }
}
