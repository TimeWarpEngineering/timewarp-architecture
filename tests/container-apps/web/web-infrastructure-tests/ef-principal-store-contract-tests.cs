// EF fixture for the shared IPrincipalStore contract (task 104-032). Uses ephemeral Postgres
// (env connection string or Testcontainers). Each CreateStore gets an independent database so
// multi-store cases stay isolated. CI fails closed when neither Docker nor a connection string
// is available (same spirit as Profile live tests); interactive hosts soft-skip via ShouldSkip.
//
// Jaribu only discovers public static Task methods on the registered type. The shared suite in
// timewarp-testing is instance-based (abstract Factory + dual Fixie/Jaribu fixtures). Concrete
// classes compose the abstract suite and re-surface each case as a static that runs a fresh
// instance (task 145-007). In-memory identity fixture uses the same wrapper pattern.

namespace PrincipalStoreContract_.Ef;

using Docker.DotNet;
using Npgsql;
using Testcontainers.PostgreSql;
using TimeWarp.Architecture.Persistence;
using TimeWarp.Identity;

file sealed class EfPrincipalStoreFactory : IPrincipalStoreFactory
{
  private static readonly Lazy<Task<PostgresAvailability>> Availability =
    new(ResolveAvailabilityAsync, LazyThreadSafetyMode.ExecutionAndPublication);

  public static bool IsAvailable
  {
    get
    {
      PostgresAvailability availability = Availability.Value.GetAwaiter().GetResult();
      if (availability.AdminConnectionString is not null)
      {
        return true;
      }

      if (IsCiEnvironment())
      {
        throw new InvalidOperationException(
          "EfPrincipalStore contract tests require a connection string or Docker under CI. " +
          (availability.SkipReason ?? "no connection"));
      }

      Console.WriteLine($"[SKIP] PrincipalStoreContract_.Ef: {availability.SkipReason ?? "no connection"}");
      return false;
    }
  }

  public IPrincipalStore CreateStore()
  {
    PostgresAvailability availability = Availability.Value.GetAwaiter().GetResult();
    if (availability.AdminConnectionString is null)
    {
      throw new InvalidOperationException(
        availability.SkipReason ?? "Postgres is not available for EfPrincipalStore contract tests.");
    }

    string databaseName = "ef_principal_" + Guid.NewGuid().ToString("N");
    CreateDatabase(availability.AdminConnectionString, databaseName);
    string connectionString = new NpgsqlConnectionStringBuilder(availability.AdminConnectionString)
    {
      Database = databaseName
    }.ConnectionString;

    DbContextOptions<PostgresDbContext> options = new DbContextOptionsBuilder<PostgresDbContext>()
      .UseNpgsql(connectionString)
      .Options;
    PostgresDbContext db = new(options);
    db.Database.EnsureCreated();
    return new EfPrincipalStore(db);
  }

  private static void CreateDatabase(string adminConnectionString, string databaseName)
  {
    // databaseName is always minted in CreateStore as "ef_principal_" + Guid.ToString("N").
    // Postgres has no parameter binding for identifiers; suppress CA2100 after the format check.
    if (databaseName.Length != 45
        || !databaseName.StartsWith("ef_principal_", StringComparison.Ordinal)
        || !databaseName.AsSpan(13).ToString().All(static c => char.IsAsciiHexDigitLower(c)))
    {
      throw new ArgumentException("Database name must be the CreateStore-minted form.", nameof(databaseName));
    }

    using NpgsqlConnection connection = new(adminConnectionString);
    connection.Open();
    using NpgsqlCommand command = connection.CreateCommand();
#pragma warning disable CA2100 // Identifier validated above; CREATE DATABASE cannot take parameters
    command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
#pragma warning restore CA2100
    command.ExecuteNonQuery();
  }

  private static bool IsCiEnvironment() =>
    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"))
    || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

  private static async Task<PostgresAvailability> ResolveAvailabilityAsync()
  {
    string? fromEnv = Environment.GetEnvironmentVariable("PostgresDbOptions__ConnectionString")
      ?? Environment.GetEnvironmentVariable("ConnectionStrings__postgres-db");

    if (!string.IsNullOrWhiteSpace(fromEnv))
    {
      var builder = new NpgsqlConnectionStringBuilder(fromEnv) { Database = "postgres" };
      return new PostgresAvailability(builder.ConnectionString, Container: null, SkipReason: null);
    }

    try
    {
      PostgreSqlContainer container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("timewarp_identity_store_tests")
        .WithUsername("timewarp")
        .WithPassword("timewarp")
        .Build();

      await container.StartAsync();
      var builder = new NpgsqlConnectionStringBuilder(container.GetConnectionString())
      {
        Database = "postgres"
      };
      return new PostgresAvailability(builder.ConnectionString, container, SkipReason: null);
    }
    catch (Exception exception) when (
      exception is DockerApiException
        or DockerContainerNotFoundException
        or HttpRequestException
        or TimeoutException
        or IOException)
    {
      string skipReason =
        "No Postgres connection available (set PostgresDbOptions__ConnectionString or " +
        "ConnectionStrings__postgres-db, or enable Docker for Testcontainers). " +
        exception.Message;
      return new PostgresAvailability(AdminConnectionString: null, Container: null, skipReason);
    }
  }

  private sealed record PostgresAvailability
  (
    string? AdminConnectionString,
    PostgreSqlContainer? Container,
    string? SkipReason
  );
}

file static class EfFixture
{
  public static readonly EfPrincipalStoreFactory Factory = new();
  public static bool ShouldSkip() => !EfPrincipalStoreFactory.IsAvailable;
}

// Composition wrappers: Jaribu discovers statics only; abstract suite methods stay instance.

public class Principals
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Principals>();

  private sealed class Suite : PrincipalStoreContract_.Principals
  {
    protected override IPrincipalStoreFactory Factory => EfFixture.Factory;
    protected override bool ShouldSkip() => EfFixture.ShouldSkip();
  }

  public static Task Add_and_get_round_trips() => new Suite().Add_and_get_round_trips();
  public static Task Duplicate_principal_id_fails() => new Suite().Duplicate_principal_id_fails();
  public static Task Update_persists_display_name_and_tier() => new Suite().Update_persists_display_name_and_tier();
  public static Task Update_missing_principal_fails() => new Suite().Update_missing_principal_fails();
}

public class Credentials
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Credentials>();

  private sealed class Suite : PrincipalStoreContract_.Credentials
  {
    protected override IPrincipalStoreFactory Factory => EfFixture.Factory;
    protected override bool ShouldSkip() => EfFixture.ShouldSkip();
  }

  public static Task First_credential_promotes_provisional_to_keyed() =>
    new Suite().First_credential_promotes_provisional_to_keyed();
  public static Task First_credential_promotes_to_keyed_even_when_quarantined() =>
    new Suite().First_credential_promotes_to_keyed_even_when_quarantined();
  public static Task Multi_credential_per_principal_is_allowed() =>
    new Suite().Multi_credential_per_principal_is_allowed();
  public static Task Find_by_handle_returns_match() => new Suite().Find_by_handle_returns_match();
  public static Task Find_by_handle_returns_revoked_credential() =>
    new Suite().Find_by_handle_returns_revoked_credential();
  public static Task Duplicate_type_and_handle_fails() => new Suite().Duplicate_type_and_handle_fails();
  public static Task Same_handle_different_type_is_allowed() =>
    new Suite().Same_handle_different_type_is_allowed();
  public static Task Missing_principal_fails_credential_add() =>
    new Suite().Missing_principal_fails_credential_add();
  public static Task List_excludes_revoked_by_default() => new Suite().List_excludes_revoked_by_default();
  public static Task Get_credential_by_id() => new Suite().Get_credential_by_id();
  public static Task Update_missing_credential_fails() => new Suite().Update_missing_credential_fails();
  public static Task Lists_in_ascending_CreatedAt_order() => new Suite().Lists_in_ascending_CreatedAt_order();
}

public class SnapshotSemantics
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SnapshotSemantics>();

  private sealed class Suite : PrincipalStoreContract_.SnapshotSemantics
  {
    protected override IPrincipalStoreFactory Factory => EfFixture.Factory;
    protected override bool ShouldSkip() => EfFixture.ShouldSkip();
  }

  public static Task Get_twice_returns_distinct_but_equal_instances() =>
    new Suite().Get_twice_returns_distinct_but_equal_instances();
  public static Task Mutating_a_snapshot_is_invisible_until_update() =>
    new Suite().Mutating_a_snapshot_is_invisible_until_update();
  public static Task Credential_byte_arrays_are_independent_across_snapshots() =>
    new Suite().Credential_byte_arrays_are_independent_across_snapshots();
  public static Task Version_is_zero_after_create_and_one_after_update() =>
    new Suite().Version_is_zero_after_create_and_one_after_update();
}

public class AddPersistsVersionAsIs
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AddPersistsVersionAsIs>();

  private sealed class Suite : PrincipalStoreContract_.AddPersistsVersionAsIs
  {
    protected override IPrincipalStoreFactory Factory => EfFixture.Factory;
    protected override bool ShouldSkip() => EfFixture.ShouldSkip();
  }

  public static Task Add_of_nonzero_version_snapshot_persists_that_version() =>
    new Suite().Add_of_nonzero_version_snapshot_persists_that_version();
}

public class StalePrincipalUpdate
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<StalePrincipalUpdate>();

  private sealed class Suite : PrincipalStoreContract_.StalePrincipalUpdate
  {
    protected override IPrincipalStoreFactory Factory => EfFixture.Factory;
    protected override bool ShouldSkip() => EfFixture.ShouldSkip();
  }

  public static Task Conflicting_update_throws_with_expected_and_actual_versions() =>
    new Suite().Conflicting_update_throws_with_expected_and_actual_versions();
  public static Task Retry_after_reGet_succeeds() => new Suite().Retry_after_reGet_succeeds();
}

public class CallerAheadConflict
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CallerAheadConflict>();

  private sealed class Suite : PrincipalStoreContract_.CallerAheadConflict
  {
    protected override IPrincipalStoreFactory Factory => EfFixture.Factory;
    protected override bool ShouldSkip() => EfFixture.ShouldSkip();
  }

  public static Task Ahead_of_store_throws_with_expected_greater_than_actual() =>
    new Suite().Ahead_of_store_throws_with_expected_greater_than_actual();
}

public class RevokeResurrectionRace
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<RevokeResurrectionRace>();

  private sealed class Suite : PrincipalStoreContract_.RevokeResurrectionRace
  {
    protected override IPrincipalStoreFactory Factory => EfFixture.Factory;
    protected override bool ShouldSkip() => EfFixture.ShouldSkip();
  }

  public static Task Stale_update_after_revoke_throws_and_store_stays_revoked() =>
    new Suite().Stale_update_after_revoke_throws_and_store_stays_revoked();
}

public class QuarantineLossRace
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<QuarantineLossRace>();

  private sealed class Suite : PrincipalStoreContract_.QuarantineLossRace
  {
    protected override IPrincipalStoreFactory Factory => EfFixture.Factory;
    protected override bool ShouldSkip() => EfFixture.ShouldSkip();
  }

  public static Task Stale_update_throws_and_store_stays_quarantined() =>
    new Suite().Stale_update_throws_and_store_stays_quarantined();
}

public class TierDemotionRace
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<TierDemotionRace>();

  private sealed class Suite : PrincipalStoreContract_.TierDemotionRace
  {
    protected override IPrincipalStoreFactory Factory => EfFixture.Factory;
    protected override bool ShouldSkip() => EfFixture.ShouldSkip();
  }

  public static Task Stale_update_throws_and_store_stays_at_promoted_tier() =>
    new Suite().Stale_update_throws_and_store_stays_at_promoted_tier();
}

public class AttachBumpsPrincipalVersion
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AttachBumpsPrincipalVersion>();

  private sealed class Suite : PrincipalStoreContract_.AttachBumpsPrincipalVersion
  {
    protected override IPrincipalStoreFactory Factory => EfFixture.Factory;
    protected override bool ShouldSkip() => EfFixture.ShouldSkip();
  }

  public static Task Pre_attach_snapshot_update_conflicts_after_first_credential() =>
    new Suite().Pre_attach_snapshot_update_conflicts_after_first_credential();
  public static Task Second_credential_add_does_not_bump_version_again() =>
    new Suite().Second_credential_add_does_not_bump_version_again();
}

public class CallerInstanceNotAdvanced
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CallerInstanceNotAdvanced>();

  private sealed class Suite : PrincipalStoreContract_.CallerInstanceNotAdvanced
  {
    protected override IPrincipalStoreFactory Factory => EfFixture.Factory;
    protected override bool ShouldSkip() => EfFixture.ShouldSkip();
  }

  public static Task Version_unchanged_on_callers_instance_after_successful_update() =>
    new Suite().Version_unchanged_on_callers_instance_after_successful_update();
  public static Task Second_update_with_same_instance_throws() =>
    new Suite().Second_update_with_same_instance_throws();
}
