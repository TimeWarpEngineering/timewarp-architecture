#region Purpose
// Host-free coverage for the last-used stamp on every Entra sign-in / link and the sync-hit account
// hint refresh (tasks 250, 252).
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

using System.Text;
using TimeWarp.Architecture.Features.Identity.Application;
using TimeWarp.Foundation.Types;
using TimeWarp.Identity;
using static EntraTicketProcessor_.EntraTicketProcessorFixture;

public class LastUsed_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<LastUsed_Given_>();

  public static async Task Sync_Hit_Should_Refresh_Account_Hint_And_Keep_It_When_Claim_Absent()
  {
    InMemoryPrincipalStore principalStore = new();
    Principal owner = Principal.Create(PrincipalKind.Human);
    await principalStore.AddPrincipalAsync(owner);
    Guid objectId = Guid.NewGuid();
    byte[] handle = EntraAccountHandle.Encode(TrustedTenantId, objectId);
    // A link made before task 250: provider label, no hint.
    await principalStore.AddCredentialAsync(
      Credential.Create(
        owner.Id,
        CredentialType.EntraAccount,
        handle,
        EntraIssuerMaterial.FromTenantId(TrustedTenantId),
        EntraIdTokenClaims.ProviderLabel));
    ManualClock clock = new(T0);
    EntraTicketProcessor processor = await ProcessorAsync(principalStore, clock);
    string issuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(TrustedTenantId));

    OneOf<PrincipalId, EntraChoiceRequired, SharedProblemDetails> signedIn = await processor.ProcessAsync(
      new EntraIdTokenClaims(TrustedTenantId, objectId, issuer, "Steven Cramer", "steve@contoso.com"),
      EntraTicketProcessor.ModeBootstrap,
      linkCallerPrincipalId: null,
      CancellationToken.None);

    signedIn.IsT0.ShouldBeTrue();
    signedIn.AsT0.ShouldBe(owner.Id);
    Credential? refreshed = await principalStore.FindCredentialByHandleAsync(CredentialType.EntraAccount, handle);
    refreshed!.AccountHint.ShouldBe("steve@contoso.com", "sign-in backfills a pre-250 link");
    long versionAfterRefresh = refreshed.Version;

    // Same value again: the hint is unchanged; the only write is the last-used stamp.
    clock.Advance(TimeSpan.FromMinutes(1));
    (await processor.ProcessAsync(
      new EntraIdTokenClaims(TrustedTenantId, objectId, issuer, "Steven Cramer", "steve@contoso.com"),
      EntraTicketProcessor.ModeBootstrap,
      linkCallerPrincipalId: null,
      CancellationToken.None)).IsT0.ShouldBeTrue();
    (await principalStore.FindCredentialByHandleAsync(CredentialType.EntraAccount, handle))!
      .Version.ShouldBe(versionAfterRefresh + 1, "an unchanged hint adds no write beyond the stamp");

    // Token without preferred_username: the stored hint is kept.
    clock.Advance(TimeSpan.FromMinutes(1));
    (await processor.ProcessAsync(
      new EntraIdTokenClaims(TrustedTenantId, objectId, issuer, "Steven Cramer"),
      EntraTicketProcessor.ModeBootstrap,
      linkCallerPrincipalId: null,
      CancellationToken.None)).IsT0.ShouldBeTrue();
    (await principalStore.FindCredentialByHandleAsync(CredentialType.EntraAccount, handle))!
      .AccountHint.ShouldBe("steve@contoso.com");

    // UPN rename upstream: the next sign-in follows it.
    clock.Advance(TimeSpan.FromMinutes(1));
    (await processor.ProcessAsync(
      new EntraIdTokenClaims(TrustedTenantId, objectId, issuer, "Steven Cramer", "steven@contoso.com"),
      EntraTicketProcessor.ModeBootstrap,
      linkCallerPrincipalId: null,
      CancellationToken.None)).IsT0.ShouldBeTrue();
    (await principalStore.FindCredentialByHandleAsync(CredentialType.EntraAccount, handle))!
      .AccountHint.ShouldBe("steven@contoso.com");
  }

  public static async Task Sync_Hit_Should_Stamp_Last_Used_And_Advance_On_Next_Sign_In()
  {
    InMemoryPrincipalStore principalStore = new();
    Principal owner = Principal.Create(PrincipalKind.Human);
    await principalStore.AddPrincipalAsync(owner);
    Guid objectId = Guid.NewGuid();
    byte[] handle = await AddEntraCredentialAsync(principalStore, owner.Id, objectId, "steve@contoso.com");
    ManualClock clock = new(T0);
    EntraTicketProcessor processor = await ProcessorAsync(principalStore, clock);
    EntraIdTokenClaims claims = Claims(objectId, "steve@contoso.com");

    (await processor.ProcessAsync(claims, EntraTicketProcessor.ModeBootstrap, null, CancellationToken.None))
      .IsT0.ShouldBeTrue();
    (await principalStore.FindCredentialByHandleAsync(CredentialType.EntraAccount, handle))!
      .LastUsedAt.ShouldBe(T0, "Microsoft 365 sign-in on an existing link stamps last-used");

    clock.Advance(TimeSpan.FromHours(1));
    (await processor.ProcessAsync(claims, EntraTicketProcessor.ModeBootstrap, null, CancellationToken.None))
      .IsT0.ShouldBeTrue();
    (await principalStore.FindCredentialByHandleAsync(CredentialType.EntraAccount, handle))!
      .LastUsedAt.ShouldBe(T0.AddHours(1), "a second sign-in advances the stamp");
  }

  public static async Task Sync_Hit_Should_Persist_Hint_And_Stamp_In_One_Write()
  {
    InMemoryPrincipalStore principalStore = new();
    Principal owner = Principal.Create(PrincipalKind.Human);
    await principalStore.AddPrincipalAsync(owner);
    Guid objectId = Guid.NewGuid();
    byte[] handle = await AddEntraCredentialAsync(principalStore, owner.Id, objectId, accountHint: null);
    long versionBefore = (await principalStore.FindCredentialByHandleAsync(CredentialType.EntraAccount, handle))!
      .Version;
    EntraTicketProcessor processor = await ProcessorAsync(principalStore);

    (await processor.ProcessAsync(
      Claims(objectId, "steve@contoso.com"),
      EntraTicketProcessor.ModeBootstrap,
      null,
      CancellationToken.None)).IsT0.ShouldBeTrue();

    Credential stored = (await principalStore.FindCredentialByHandleAsync(CredentialType.EntraAccount, handle))!;
    stored.AccountHint.ShouldBe("steve@contoso.com");
    stored.LastUsedAt.ShouldBe(T0);
    stored.Version.ShouldBe(versionBefore + 1, "hint refresh and last-used stamp persist in one UPDATE");
  }

  public static async Task Bootstrap_Create_On_Already_Linked_Handle_Should_Stamp_Last_Used()
  {
    InMemoryPrincipalStore principalStore = new();
    Principal owner = Principal.Create(PrincipalKind.Human);
    await principalStore.AddPrincipalAsync(owner);
    Guid objectId = Guid.NewGuid();
    byte[] handle = await AddEntraCredentialAsync(principalStore, owner.Id, objectId, "steve@contoso.com");
    EntraTicketProcessor processor = await ProcessorAsync(principalStore);

    OneOf<PrincipalId, SharedProblemDetails> result = await processor.CompleteBootstrapCreateAsync(
      Claims(objectId, "steve@contoso.com"),
      CancellationToken.None);

    result.AsT0.ShouldBe(owner.Id);
    (await principalStore.FindCredentialByHandleAsync(CredentialType.EntraAccount, handle))!
      .LastUsedAt.ShouldBe(T0);
  }

  public static async Task Link_And_Bootstrap_Mint_Should_Stamp_Last_Used()
  {
    InMemoryPrincipalStore principalStore = new();
    Principal caller = Principal.Create(PrincipalKind.Human);
    await principalStore.AddPrincipalAsync(caller);
    EntraTicketProcessor processor = await ProcessorAsync(principalStore);
    Guid linkedObjectId = Guid.NewGuid();
    Guid mintedObjectId = Guid.NewGuid();

    (await processor.ProcessAsync(
      Claims(linkedObjectId, "steve@contoso.com"),
      EntraTicketProcessor.ModeLink,
      caller.Id,
      CancellationToken.None)).IsT0.ShouldBeTrue();
    (await processor.CompleteBootstrapCreateAsync(
      Claims(mintedObjectId, "new@contoso.com"),
      CancellationToken.None)).IsT0.ShouldBeTrue();

    (await principalStore.FindCredentialByHandleAsync(
      CredentialType.EntraAccount,
      EntraAccountHandle.Encode(TrustedTenantId, linkedObjectId)))!.LastUsedAt.ShouldBe(T0, "linking stamps");
    (await principalStore.FindCredentialByHandleAsync(
      CredentialType.EntraAccount,
      EntraAccountHandle.Encode(TrustedTenantId, mintedObjectId)))!.LastUsedAt.ShouldBe(T0, "bootstrap mint stamps");
  }

  public static async Task Link_Merge_Of_Foreign_Owner_Should_Stamp_Last_Used()
  {
    InMemoryPrincipalStore principalStore = new();
    Principal owner = Principal.Create(PrincipalKind.Human);
    Principal caller = Principal.Create(PrincipalKind.Human);
    await principalStore.AddPrincipalAsync(owner);
    await principalStore.AddPrincipalAsync(caller);
    Guid objectId = Guid.NewGuid();
    byte[] handle = await AddEntraCredentialAsync(principalStore, owner.Id, objectId, accountHint: null);
    EntraTicketProcessor processor = await ProcessorAsync(principalStore);

    (await processor.ProcessAsync(
      Claims(objectId, "steve@contoso.com"),
      EntraTicketProcessor.ModeLink,
      caller.Id,
      CancellationToken.None)).AsT0.ShouldBe(caller.Id);

    Credential stored = (await principalStore.FindCredentialByHandleAsync(CredentialType.EntraAccount, handle))!;
    stored.PrincipalId.ShouldBe(caller.Id);
    stored.LastUsedAt.ShouldBe(T0);
    stored.AccountHint.ShouldBe("steve@contoso.com");
  }

  public static async Task Quarantined_Principal_Should_Not_Stamp_Last_Used()
  {
    InMemoryPrincipalStore principalStore = new();
    Principal owner = Principal.Create(PrincipalKind.Human);
    owner.Quarantine();
    await principalStore.AddPrincipalAsync(owner);
    Guid objectId = Guid.NewGuid();
    byte[] handle = await AddEntraCredentialAsync(principalStore, owner.Id, objectId, "steve@contoso.com");
    long versionBefore = (await principalStore.FindCredentialByHandleAsync(CredentialType.EntraAccount, handle))!
      .Version;
    EntraTicketProcessor processor = await ProcessorAsync(principalStore);

    OneOf<PrincipalId, EntraChoiceRequired, SharedProblemDetails> signIn = await processor.ProcessAsync(
      Claims(objectId, "renamed@contoso.com"),
      EntraTicketProcessor.ModeBootstrap,
      null,
      CancellationToken.None);
    OneOf<PrincipalId, SharedProblemDetails> bootstrap = await processor.CompleteBootstrapCreateAsync(
      Claims(objectId, "renamed@contoso.com"),
      CancellationToken.None);

    signIn.IsT2.ShouldBeTrue();
    bootstrap.IsT1.ShouldBeTrue();
    Credential stored = (await principalStore.FindCredentialByHandleAsync(CredentialType.EntraAccount, handle))!;
    stored.LastUsedAt.ShouldBeNull("a quarantined principal never stamps");
    stored.AccountHint.ShouldBe("steve@contoso.com");
    stored.Version.ShouldBe(versionBefore);
  }
}
