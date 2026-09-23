#region Purpose
// Task 248-002: Credential.LastUsedAt / MarkUsed — null at create, monotonic, copied by Snapshot,
// persisted by the in-memory store's Update CAS.
#endregion

namespace CredentialLastUsed_;

public class MarkUsed_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<MarkUsed_Should_>();

  private static readonly DateTimeOffset T0 = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

  public static Task Start_Null_At_Create()
  {
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], [2]);
    credential.LastUsedAt.ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Set_The_Stamp_And_Report_Advance()
  {
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], [2]);

    credential.MarkUsed(T0).ShouldBeTrue();

    credential.LastUsedAt.ShouldBe(T0);
    return Task.CompletedTask;
  }

  public static Task Be_Monotonic_Ignoring_Earlier_Or_Equal_Instants()
  {
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.AgentKey, [1], [2]);
    credential.MarkUsed(T0);

    credential.MarkUsed(T0).ShouldBeFalse("same instant must not count as an advance");
    credential.MarkUsed(T0.AddMinutes(-1)).ShouldBeFalse("a stale instant must never rewind the stamp");
    credential.LastUsedAt.ShouldBe(T0);

    credential.MarkUsed(T0.AddMinutes(1)).ShouldBeTrue();
    credential.LastUsedAt.ShouldBe(T0.AddMinutes(1));
    return Task.CompletedTask;
  }

  public static Task Not_Care_About_Revocation()
  {
    // Advisory state: the authentication ladder rejects revoked credentials BEFORE any write point
    // runs; the domain mutation itself carries no revoke guard (Credential's Design region).
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], [2]);
    credential.Revoke();

    credential.MarkUsed(T0).ShouldBeTrue();
    credential.LastUsedAt.ShouldBe(T0);
    return Task.CompletedTask;
  }

  public static Task Be_Copied_By_Snapshot()
  {
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], [2]);
    credential.MarkUsed(T0);

    Credential snapshot = credential.Snapshot(7);

    snapshot.LastUsedAt.ShouldBe(T0);
    snapshot.Version.ShouldBe(7);
    return Task.CompletedTask;
  }

  public static async Task Persist_Through_The_Store_Update_Cas()
  {
    var store = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);
    Credential credential = Credential.Create(principal.Id, CredentialType.Passkey, [1], [2]);
    await store.AddCredentialAsync(credential);

    Credential inHand = (await store.GetCredentialAsync(credential.Id))!;
    inHand.MarkUsed(T0);
    await store.UpdateCredentialAsync(inHand);

    Credential reloaded = (await store.GetCredentialAsync(credential.Id))!;
    reloaded.LastUsedAt.ShouldBe(T0);
    reloaded.Version.ShouldBe(1);
    (await store.FindCredentialByHandleAsync(CredentialType.Passkey, [1]))!.LastUsedAt.ShouldBe(T0);
    (await store.ListCredentialsAsync(principal.Id))[0].LastUsedAt.ShouldBe(T0);
  }
}
