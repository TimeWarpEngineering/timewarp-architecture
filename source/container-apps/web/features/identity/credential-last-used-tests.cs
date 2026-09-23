#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0005;IDE0007;IDE0008

// Co-located Jaribu: task 248-002 — last-used stamp vs RevokeCredential's retry loop (revoke wins in
// both orderings, no exception reaches either caller) and GetCredentials surfacing LastUsedAt.
// Run standalone:  dotnet run source/container-apps/web/features/identity/credential-last-used-tests.cs

#region Purpose
// Jaribu runfile: a concurrent MarkUsed write never beats or breaks RevokeCredential — the revoke
// handler's snapshot + Version retry absorbs a stamp that landed first, and a stamp that lands
// second is dropped by CredentialUsageRecorder; GetCredentials round-trips LastUsedAt (null = never).
#endregion

#region Design
// Handler-seam tests against a real InMemoryPrincipalStore (same posture as
// revoke-credential-concurrency-retry-tests): the two orderings are forced deterministically —
// "stamp first" by an interleaving store decorator that performs the recorder's write from inside
// the revoke handler's FIRST UpdateCredentialAsync (so the handler's in-hand snapshot is stale by
// the time it writes), "revoke first" by simply running the handler to completion before the
// recorder writes with a pre-revoke snapshot.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Identity
{

  using System.Threading.Tasks;
  using Shouldly;
  using TimeWarp.Architecture.Abstractions;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using GetCredentialsHandler = TimeWarp.Architecture.Features.Identity.Application.GetCredentials.Handler;
  using RevokeHandler = TimeWarp.Architecture.Features.Identity.Application.RevokeCredential.Handler;

  [TestTag("Concurrency")]
  public class LastUsedVersusRevoke_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<LastUsedVersusRevoke_Given_>();

    private static readonly DateTimeOffset T0 = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    public static async Task Revoke_Lands_First_Should_Drop_The_Stamp_And_Keep_The_Row_Revoked()
    {
      (InMemoryPrincipalStore store, PrincipalId owner, Credential target) = await SeedAsync();
      var recorder = new CredentialUsageRecorder(new ManualClock(T0));
      Credential preRevokeSnapshot = (await store.GetCredentialAsync(target.Id))!;

      RevokeHandler revoke = new(store, new StubCurrentPrincipalAccessor(owner));
      OneOf.OneOf<RevokeCredential.Response, SharedProblemDetails> revoked =
        await revoke.Handle(new RevokeCredential.Command { UserId = Guid.NewGuid(), CredentialId = target.Id.Value }, CancellationToken.None);
      revoked.IsT0.ShouldBeTrue();

      bool written = await recorder.RecordAsync(store, preRevokeSnapshot);

      written.ShouldBeFalse("the stale stamp lost the Version race and is dropped, not retried");
      Credential stored = (await store.GetCredentialAsync(target.Id))!;
      stored.IsRevoked.ShouldBeTrue();
      stored.LastUsedAt.ShouldBeNull();
      stored.Version.ShouldBe(1, "only the revoke wrote");
    }

    public static async Task Stamp_Lands_First_Should_Make_Revoke_Retry_And_Still_Win()
    {
      (InMemoryPrincipalStore store, PrincipalId owner, Credential target) = await SeedAsync();
      var recorder = new CredentialUsageRecorder(new ManualClock(T0));
      var interleaving = new StampBeforeFirstUpdateStore(store, recorder, target.Id);

      RevokeHandler revoke = new(interleaving, new StubCurrentPrincipalAccessor(owner));
      OneOf.OneOf<RevokeCredential.Response, SharedProblemDetails> result =
        await revoke.Handle(new RevokeCredential.Command { UserId = Guid.NewGuid(), CredentialId = target.Id.Value }, CancellationToken.None);

      result.IsT0.ShouldBeTrue("one stale conflict caused by the stamp is absorbed by the revoke retry loop");
      interleaving.StampWritten.ShouldBeTrue();
      interleaving.UpdateAttempts.ShouldBe(2, "first attempt conflicted on the stamp, second succeeded");

      Credential stored = (await store.GetCredentialAsync(target.Id))!;
      stored.IsRevoked.ShouldBeTrue("revoke wins");
      stored.LastUsedAt.ShouldBe(T0, "the earlier stamp is retained on the revoked row");
      stored.Version.ShouldBe(2);
    }

    public static async Task Request_Path_After_Revoke_Should_Not_Write()
    {
      (InMemoryPrincipalStore store, PrincipalId owner, Credential target) = await SeedAsync();
      var recorder = new CredentialUsageRecorder(new ManualClock(T0));
      RevokeHandler revoke = new(store, new StubCurrentPrincipalAccessor(owner));
      (await revoke.Handle(new RevokeCredential.Command { UserId = Guid.NewGuid(), CredentialId = target.Id.Value }, CancellationToken.None)).IsT0.ShouldBeTrue();

      (await recorder.RecordCoalescedAsync(store, target.Id)).ShouldBeFalse();
      (await store.GetCredentialAsync(target.Id))!.Version.ShouldBe(1);
    }

    private static async Task<(InMemoryPrincipalStore Store, PrincipalId Owner, Credential Target)> SeedAsync()
    {
      InMemoryPrincipalStore store = new();
      Principal principal = Principal.Create(PrincipalKind.Human);
      await store.AddPrincipalAsync(principal);
      Credential target = Credential.Create(principal.Id, CredentialType.Passkey, [1], [1], "Proton Pass");
      Credential sibling = Credential.Create(principal.Id, CredentialType.Passkey, [2], [2]);
      await store.AddCredentialAsync(target);
      await store.AddCredentialAsync(sibling);
      return (store, principal.Id, target);
    }

    /// <summary>Performs the recorder's stamp from inside the revoke handler's first Update so that handler's snapshot goes stale.</summary>
    private sealed class StampBeforeFirstUpdateStore : IPrincipalStore
    {
      private readonly IPrincipalStore Inner;
      private readonly CredentialUsageRecorder Recorder;
      private readonly CredentialId TargetId;

      public StampBeforeFirstUpdateStore(IPrincipalStore inner, CredentialUsageRecorder recorder, CredentialId targetId)
      {
        Inner = inner;
        Recorder = recorder;
        TargetId = targetId;
      }

      public int UpdateAttempts { get; private set; }
      public bool StampWritten { get; private set; }

      public async Task UpdateCredentialAsync(Credential credential, CancellationToken cancellationToken = default)
      {
        UpdateAttempts++;
        if (UpdateAttempts == 1)
        {
          Credential fresh = (await Inner.GetCredentialAsync(TargetId, cancellationToken))!;
          StampWritten = await Recorder.RecordAsync(Inner, fresh, cancellationToken);
        }

        await Inner.UpdateCredentialAsync(credential, cancellationToken);
      }

      public Task AddPrincipalAsync(Principal principal, CancellationToken cancellationToken = default) => Inner.AddPrincipalAsync(principal, cancellationToken);
      public Task<Principal?> GetPrincipalAsync(PrincipalId id, CancellationToken cancellationToken = default) => Inner.GetPrincipalAsync(id, cancellationToken);
      public Task UpdatePrincipalAsync(Principal principal, CancellationToken cancellationToken = default) => Inner.UpdatePrincipalAsync(principal, cancellationToken);
      public Task<IReadOnlyList<Principal>> ListPrincipalsAsync(CancellationToken cancellationToken = default) => Inner.ListPrincipalsAsync(cancellationToken);
      public Task AddCredentialAsync(Credential credential, CancellationToken cancellationToken = default) => Inner.AddCredentialAsync(credential, cancellationToken);
      public Task<Credential?> GetCredentialAsync(CredentialId credentialId, CancellationToken cancellationToken = default) => Inner.GetCredentialAsync(credentialId, cancellationToken);
      public Task<Credential?> FindCredentialByHandleAsync(CredentialType type, byte[] handle, CancellationToken cancellationToken = default) => Inner.FindCredentialByHandleAsync(type, handle, cancellationToken);
      public Task<IReadOnlyList<Credential>> ListCredentialsAsync(PrincipalId principalId, bool includeRevoked = false, CancellationToken cancellationToken = default) => Inner.ListCredentialsAsync(principalId, includeRevoked, cancellationToken);
      public Task MergePrincipalAsync(PrincipalId sourceId, PrincipalId targetId, CancellationToken cancellationToken = default) => Inner.MergePrincipalAsync(sourceId, targetId, cancellationToken);
    }
  }

  [TestTag("Handler")]
  public class GetCredentialsLastUsed_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<GetCredentialsLastUsed_Given_>();

    public static async Task Used_And_Never_Used_Rows_Should_Round_Trip_LastUsedAt()
    {
      InMemoryPrincipalStore store = new();
      Principal principal = Principal.Create(PrincipalKind.Human);
      await store.AddPrincipalAsync(principal);
      Credential used = Credential.Create(principal.Id, CredentialType.Passkey, [1], [1]);
      Credential neverUsed = Credential.Create(principal.Id, CredentialType.AgentKey, [2], [2]);
      await store.AddCredentialAsync(used);
      await store.AddCredentialAsync(neverUsed);

      DateTimeOffset stamp = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);
      var recorder = new CredentialUsageRecorder(new ManualClock(stamp));
      (await recorder.RecordAsync(store, (await store.GetCredentialAsync(used.Id))!)).ShouldBeTrue();

      GetCredentialsHandler handler = new(store, new StubCurrentPrincipalAccessor(principal.Id));
      OneOf.OneOf<GetCredentials.Response, SharedProblemDetails> result =
        await handler.Handle(new GetCredentials.Query { UserId = Guid.NewGuid() }, CancellationToken.None);

      result.IsT0.ShouldBeTrue();
      GetCredentials.CredentialSummary usedRow = result.AsT0.Credentials.Single(row => row.Id == used.Id);
      GetCredentials.CredentialSummary neverUsedRow = result.AsT0.Credentials.Single(row => row.Id == neverUsed.Id);
      usedRow.LastUsedAt.ShouldBe(stamp);
      neverUsedRow.LastUsedAt.ShouldBeNull();
    }
  }

  internal sealed class ManualClock : TimeProvider
  {
    private readonly DateTimeOffset Now;

    public ManualClock(DateTimeOffset now) => Now = now;

    public override DateTimeOffset GetUtcNow() => Now;
  }

  internal sealed class StubCurrentPrincipalAccessor : ICurrentPrincipalAccessor
  {
    private readonly PrincipalId? PrincipalId;

    public StubCurrentPrincipalAccessor(PrincipalId? principalId) => PrincipalId = principalId;

    public Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken cancellationToken) =>
      Task.FromResult(PrincipalId);
  }
}
