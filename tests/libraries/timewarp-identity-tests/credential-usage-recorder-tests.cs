#region Purpose
// Task 248-002: CredentialUsageRecorder — per-ceremony writes every time, per-request writes are
// coalesced to one per interval per credential, and a write that loses the version race is dropped
// (no exception, no retry) so a concurrent revoke always wins.
#endregion

#region Design
// Deterministic clock: a ManualClock TimeProvider so "inside the interval" / "after the interval"
// are exact instants, not sleeps. Writes are counted through Credential.Version — the store bumps it
// by exactly one per successful Update (IPrincipalStore's Design region), so "one write" is
// "Version advanced by one", which is the observable the hot-path requirement is about.
#endregion

namespace CredentialUsageRecorder_;

public class Record_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Record_Should_>();

  private static readonly DateTimeOffset T0 = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

  public static async Task Write_Every_Ceremony_Call()
  {
    (InMemoryPrincipalStore store, Credential credential, ManualClock clock) = await SeedAsync();
    var recorder = new CredentialUsageRecorder(clock, TimeSpan.FromMinutes(5));

    (await recorder.RecordAsync(store, (await store.GetCredentialAsync(credential.Id))!)).ShouldBeTrue();
    clock.Now = T0.AddSeconds(10);
    (await recorder.RecordAsync(store, (await store.GetCredentialAsync(credential.Id))!)).ShouldBeTrue("a sign-in is per-ceremony: no coalescing on this path");

    Credential stored = (await store.GetCredentialAsync(credential.Id))!;
    stored.LastUsedAt.ShouldBe(T0.AddSeconds(10));
    stored.Version.ShouldBe(2);
  }

  public static async Task Coalesce_Two_Requests_Inside_The_Interval_To_One_Write()
  {
    (InMemoryPrincipalStore store, Credential credential, ManualClock clock) = await SeedAsync();
    var recorder = new CredentialUsageRecorder(clock, TimeSpan.FromMinutes(5));

    (await recorder.RecordCoalescedAsync(store, credential.Id)).ShouldBeTrue();
    clock.Now = T0.AddMinutes(1);
    (await recorder.RecordCoalescedAsync(store, credential.Id)).ShouldBeFalse("inside the interval: no store round-trip");

    Credential stored = (await store.GetCredentialAsync(credential.Id))!;
    stored.Version.ShouldBe(1, "exactly one write for two requests inside the interval");
    stored.LastUsedAt.ShouldBe(T0);
  }

  public static async Task Write_Again_Once_The_Interval_Has_Elapsed()
  {
    (InMemoryPrincipalStore store, Credential credential, ManualClock clock) = await SeedAsync();
    var recorder = new CredentialUsageRecorder(clock, TimeSpan.FromMinutes(5));

    await recorder.RecordCoalescedAsync(store, credential.Id);
    clock.Now = T0.AddMinutes(5);
    (await recorder.RecordCoalescedAsync(store, credential.Id)).ShouldBeTrue("interval boundary is inclusive: >= interval writes");

    Credential stored = (await store.GetCredentialAsync(credential.Id))!;
    stored.Version.ShouldBe(2);
    stored.LastUsedAt.ShouldBe(T0.AddMinutes(5));
  }

  public static async Task Share_The_Coalescing_Window_Between_Ceremony_And_Request_Paths()
  {
    (InMemoryPrincipalStore store, Credential credential, ManualClock clock) = await SeedAsync();
    var recorder = new CredentialUsageRecorder(clock, TimeSpan.FromMinutes(5));

    // Token issuance (ceremony write) followed immediately by bearer use: one write, not two.
    await recorder.RecordAsync(store, (await store.GetCredentialAsync(credential.Id))!);
    clock.Now = T0.AddSeconds(1);
    (await recorder.RecordCoalescedAsync(store, credential.Id)).ShouldBeFalse();

    (await store.GetCredentialAsync(credential.Id))!.Version.ShouldBe(1);
  }

  public static async Task Coalesce_Per_Credential_Not_Globally()
  {
    (InMemoryPrincipalStore store, Credential first, ManualClock clock) = await SeedAsync();
    Credential second = Credential.Create(first.PrincipalId, CredentialType.AgentKey, [9], [9]);
    await store.AddCredentialAsync(second);
    var recorder = new CredentialUsageRecorder(clock, TimeSpan.FromMinutes(5));

    (await recorder.RecordCoalescedAsync(store, first.Id)).ShouldBeTrue();
    (await recorder.RecordCoalescedAsync(store, second.Id)).ShouldBeTrue("a different credential has its own window");
  }

  public static async Task Drop_A_Write_That_Loses_The_Race_To_A_Revoke_Without_Throwing()
  {
    // Ordering A: the revoke lands first; the recorder still holds the pre-revoke snapshot.
    (InMemoryPrincipalStore store, Credential credential, ManualClock clock) = await SeedAsync();
    var recorder = new CredentialUsageRecorder(clock, TimeSpan.FromMinutes(5));
    Credential staleSnapshot = (await store.GetCredentialAsync(credential.Id))!;

    Credential revoking = (await store.GetCredentialAsync(credential.Id))!;
    revoking.Revoke();
    await store.UpdateCredentialAsync(revoking);

    bool written = await recorder.RecordAsync(store, staleSnapshot);

    written.ShouldBeFalse("a lost version race is dropped, never retried");
    Credential stored = (await store.GetCredentialAsync(credential.Id))!;
    stored.IsRevoked.ShouldBeTrue("revoke wins");
    stored.LastUsedAt.ShouldBeNull("the dropped stamp must not have been persisted");
    stored.Version.ShouldBe(1, "only the revoke wrote");
  }

  public static async Task Not_Write_On_The_Request_Path_For_A_Revoked_Or_Unknown_Credential()
  {
    (InMemoryPrincipalStore store, Credential credential, ManualClock clock) = await SeedAsync();
    var recorder = new CredentialUsageRecorder(clock, TimeSpan.FromMinutes(5));
    Credential revoking = (await store.GetCredentialAsync(credential.Id))!;
    revoking.Revoke();
    await store.UpdateCredentialAsync(revoking);

    (await recorder.RecordCoalescedAsync(store, credential.Id)).ShouldBeFalse();
    (await recorder.RecordCoalescedAsync(store, CredentialId.New())).ShouldBeFalse("unknown id is a no-op, not an exception");

    (await store.GetCredentialAsync(credential.Id))!.Version.ShouldBe(1);
  }

  public static async Task Let_Non_Race_Store_Failures_Propagate()
  {
    (InMemoryPrincipalStore store, Credential credential, ManualClock clock) = await SeedAsync();
    var recorder = new CredentialUsageRecorder(clock, TimeSpan.FromMinutes(5));
    Credential neverAdded = Credential.Create(credential.PrincipalId, CredentialType.Passkey, [42], [42]);

    // "Does not exist" is InvalidOperationException, a distinct failure class from staleness — the
    // recorder only swallows ConcurrencyConflictException (its Design region).
    await Should.ThrowAsync<InvalidOperationException>(() => recorder.RecordAsync(store, neverAdded));
  }

  public static Task Reject_A_Negative_Interval_And_Default_To_Five_Minutes()
  {
    Should.Throw<ArgumentOutOfRangeException>(() => new CredentialUsageRecorder(coalesceInterval: TimeSpan.FromSeconds(-1)));
    new CredentialUsageRecorder().CoalesceInterval.ShouldBe(TimeSpan.FromMinutes(5));
    CredentialUsageRecorder.DefaultCoalesceInterval.ShouldBe(TimeSpan.FromMinutes(5));
    return Task.CompletedTask;
  }

  private static async Task<(InMemoryPrincipalStore Store, Credential Credential, ManualClock Clock)> SeedAsync()
  {
    var store = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);
    Credential credential = Credential.Create(principal.Id, CredentialType.Passkey, [1], [2]);
    await store.AddCredentialAsync(credential);
    return (store, credential, new ManualClock { Now = T0 });
  }

  private sealed class ManualClock : TimeProvider
  {
    public DateTimeOffset Now { get; set; }

    public override DateTimeOffset GetUtcNow() => Now;
  }
}
