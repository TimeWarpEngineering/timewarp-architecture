// In-memory fixture for the shared IPrincipalStore contract (task 104-032).
// Jaribu discovers only public static new Task methods on the registered type — leaf classes
// re-export each base instance case as a static wrapper (cast to base to avoid CS0176 when
// the static method hides the inherited instance name). Abstract bases stay instance-shaped
// shared instance suite in timewarp-testing; both fixtures re-export static wrappers (task 145-007).

namespace PrincipalStoreContract_.InMemory;

file sealed class InMemoryPrincipalStoreFactory : IPrincipalStoreFactory
{
  public IPrincipalStore CreateStore() => new InMemoryPrincipalStore();
}

file static class Fixture
{
  public static readonly IPrincipalStoreFactory Factory = new InMemoryPrincipalStoreFactory();
}

public class Principals : PrincipalStoreContract_.Principals
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Principals>();

  protected override IPrincipalStoreFactory Factory => Fixture.Factory;

  public static new Task Add_and_get_round_trips() =>
    ((PrincipalStoreContract_.Principals)new Principals()).Add_and_get_round_trips();

  public static new Task Duplicate_principal_id_fails() =>
    ((PrincipalStoreContract_.Principals)new Principals()).Duplicate_principal_id_fails();

  public static new Task Update_persists_display_name_and_tier() =>
    ((PrincipalStoreContract_.Principals)new Principals()).Update_persists_display_name_and_tier();

  public static new Task Update_missing_principal_fails() =>
    ((PrincipalStoreContract_.Principals)new Principals()).Update_missing_principal_fails();

  public static new Task List_principals_returns_snapshots_ordered_by_created_at() =>
    ((PrincipalStoreContract_.Principals)new Principals()).List_principals_returns_snapshots_ordered_by_created_at();

  public static new Task List_principals_empty_store_returns_empty() =>
    ((PrincipalStoreContract_.Principals)new Principals()).List_principals_empty_store_returns_empty();
}

public class Credentials : PrincipalStoreContract_.Credentials
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Credentials>();

  protected override IPrincipalStoreFactory Factory => Fixture.Factory;

  public static new Task First_credential_promotes_provisional_to_keyed() =>
    ((PrincipalStoreContract_.Credentials)new Credentials()).First_credential_promotes_provisional_to_keyed();

  public static new Task First_credential_promotes_to_keyed_even_when_quarantined() =>
    ((PrincipalStoreContract_.Credentials)new Credentials()).First_credential_promotes_to_keyed_even_when_quarantined();

  public static new Task Multi_credential_per_principal_is_allowed() =>
    ((PrincipalStoreContract_.Credentials)new Credentials()).Multi_credential_per_principal_is_allowed();

  public static new Task Find_by_handle_returns_match() =>
    ((PrincipalStoreContract_.Credentials)new Credentials()).Find_by_handle_returns_match();

  public static new Task Find_by_handle_returns_revoked_credential() =>
    ((PrincipalStoreContract_.Credentials)new Credentials()).Find_by_handle_returns_revoked_credential();

  public static new Task Duplicate_type_and_handle_fails() =>
    ((PrincipalStoreContract_.Credentials)new Credentials()).Duplicate_type_and_handle_fails();

  public static new Task Same_handle_different_type_is_allowed() =>
    ((PrincipalStoreContract_.Credentials)new Credentials()).Same_handle_different_type_is_allowed();

  public static new Task Missing_principal_fails_credential_add() =>
    ((PrincipalStoreContract_.Credentials)new Credentials()).Missing_principal_fails_credential_add();

  public static new Task List_excludes_revoked_by_default() =>
    ((PrincipalStoreContract_.Credentials)new Credentials()).List_excludes_revoked_by_default();

  public static new Task Get_credential_by_id() =>
    ((PrincipalStoreContract_.Credentials)new Credentials()).Get_credential_by_id();

  public static new Task Update_missing_credential_fails() =>
    ((PrincipalStoreContract_.Credentials)new Credentials()).Update_missing_credential_fails();

  public static new Task Lists_in_ascending_CreatedAt_order() =>
    ((PrincipalStoreContract_.Credentials)new Credentials()).Lists_in_ascending_CreatedAt_order();
}

public class SnapshotSemantics : PrincipalStoreContract_.SnapshotSemantics
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SnapshotSemantics>();

  protected override IPrincipalStoreFactory Factory => Fixture.Factory;

  public static new Task Get_twice_returns_distinct_but_equal_instances() =>
    ((PrincipalStoreContract_.SnapshotSemantics)new SnapshotSemantics()).Get_twice_returns_distinct_but_equal_instances();

  public static new Task Mutating_a_snapshot_is_invisible_until_update() =>
    ((PrincipalStoreContract_.SnapshotSemantics)new SnapshotSemantics()).Mutating_a_snapshot_is_invisible_until_update();

  public static new Task Credential_byte_arrays_are_independent_across_snapshots() =>
    ((PrincipalStoreContract_.SnapshotSemantics)new SnapshotSemantics()).Credential_byte_arrays_are_independent_across_snapshots();

  public static new Task Version_is_zero_after_create_and_one_after_update() =>
    ((PrincipalStoreContract_.SnapshotSemantics)new SnapshotSemantics()).Version_is_zero_after_create_and_one_after_update();
}

public class AddPersistsVersionAsIs : PrincipalStoreContract_.AddPersistsVersionAsIs
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AddPersistsVersionAsIs>();

  protected override IPrincipalStoreFactory Factory => Fixture.Factory;

  public static new Task Add_of_nonzero_version_snapshot_persists_that_version() =>
    ((PrincipalStoreContract_.AddPersistsVersionAsIs)new AddPersistsVersionAsIs()).Add_of_nonzero_version_snapshot_persists_that_version();
}

public class StalePrincipalUpdate : PrincipalStoreContract_.StalePrincipalUpdate
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<StalePrincipalUpdate>();

  protected override IPrincipalStoreFactory Factory => Fixture.Factory;

  public static new Task Conflicting_update_throws_with_expected_and_actual_versions() =>
    ((PrincipalStoreContract_.StalePrincipalUpdate)new StalePrincipalUpdate()).Conflicting_update_throws_with_expected_and_actual_versions();

  public static new Task Retry_after_reGet_succeeds() =>
    ((PrincipalStoreContract_.StalePrincipalUpdate)new StalePrincipalUpdate()).Retry_after_reGet_succeeds();
}

public class CallerAheadConflict : PrincipalStoreContract_.CallerAheadConflict
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CallerAheadConflict>();

  protected override IPrincipalStoreFactory Factory => Fixture.Factory;

  public static new Task Ahead_of_store_throws_with_expected_greater_than_actual() =>
    ((PrincipalStoreContract_.CallerAheadConflict)new CallerAheadConflict()).Ahead_of_store_throws_with_expected_greater_than_actual();
}

public class RevokeResurrectionRace : PrincipalStoreContract_.RevokeResurrectionRace
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<RevokeResurrectionRace>();

  protected override IPrincipalStoreFactory Factory => Fixture.Factory;

  public static new Task Stale_update_after_revoke_throws_and_store_stays_revoked() =>
    ((PrincipalStoreContract_.RevokeResurrectionRace)new RevokeResurrectionRace()).Stale_update_after_revoke_throws_and_store_stays_revoked();
}

public class QuarantineLossRace : PrincipalStoreContract_.QuarantineLossRace
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<QuarantineLossRace>();

  protected override IPrincipalStoreFactory Factory => Fixture.Factory;

  public static new Task Stale_update_throws_and_store_stays_quarantined() =>
    ((PrincipalStoreContract_.QuarantineLossRace)new QuarantineLossRace()).Stale_update_throws_and_store_stays_quarantined();
}

public class TierDemotionRace : PrincipalStoreContract_.TierDemotionRace
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<TierDemotionRace>();

  protected override IPrincipalStoreFactory Factory => Fixture.Factory;

  public static new Task Stale_update_throws_and_store_stays_at_promoted_tier() =>
    ((PrincipalStoreContract_.TierDemotionRace)new TierDemotionRace()).Stale_update_throws_and_store_stays_at_promoted_tier();
}

public class AttachBumpsPrincipalVersion : PrincipalStoreContract_.AttachBumpsPrincipalVersion
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AttachBumpsPrincipalVersion>();

  protected override IPrincipalStoreFactory Factory => Fixture.Factory;

  public static new Task Pre_attach_snapshot_update_conflicts_after_first_credential() =>
    ((PrincipalStoreContract_.AttachBumpsPrincipalVersion)new AttachBumpsPrincipalVersion()).Pre_attach_snapshot_update_conflicts_after_first_credential();

  public static new Task Second_credential_add_does_not_bump_version_again() =>
    ((PrincipalStoreContract_.AttachBumpsPrincipalVersion)new AttachBumpsPrincipalVersion()).Second_credential_add_does_not_bump_version_again();
}

public class CallerInstanceNotAdvanced : PrincipalStoreContract_.CallerInstanceNotAdvanced
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CallerInstanceNotAdvanced>();

  protected override IPrincipalStoreFactory Factory => Fixture.Factory;

  public static new Task Version_unchanged_on_callers_instance_after_successful_update() =>
    ((PrincipalStoreContract_.CallerInstanceNotAdvanced)new CallerInstanceNotAdvanced()).Version_unchanged_on_callers_instance_after_successful_update();

  public static new Task Second_update_with_same_instance_throws() =>
    ((PrincipalStoreContract_.CallerInstanceNotAdvanced)new CallerInstanceNotAdvanced()).Second_update_with_same_instance_throws();
}
