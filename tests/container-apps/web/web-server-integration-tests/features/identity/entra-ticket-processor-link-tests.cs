#region Purpose
// Host-free coverage for EntraTicketProcessor link mode: merge of a foreign owner, revoked-handle and
// foreign-tenant refusals, already-linked 409s, and the preferred_username AccountHint (task 250).
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

public class Link_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Link_Given_>();

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

  public static async Task Link_Should_Store_Preferred_Username_As_Account_Hint()
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
    stored!.Label.ShouldBe(EntraIdTokenClaims.ProviderLabel);
    stored.AccountHint.ShouldBe("Steven.Cramer@TimeWarp.Enterprises");
    stored.Handle.ShouldBe(EntraAccountHandle.Encode(TrustedTenantId, objectId), "the join key stays tid:oid");
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
}
