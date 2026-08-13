// EF IPrincipalRoleStore durability (task 147-006). Ephemeral Postgres (env or Testcontainers).
// Soft-skip when unavailable interactively; CI fails closed when neither Docker nor connection.

namespace PrincipalRoleStore_.Ef;

using Docker.DotNet;
using Npgsql;
using Testcontainers.PostgreSql;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Admin.Principals.Infrastructure;
using TimeWarp.Architecture.Persistence;
using TimeWarp.Identity;

file sealed class EfPrincipalRoleStoreFactory
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
          "EfPrincipalRoleStore tests require a connection string or Docker under CI. " +
          (availability.SkipReason ?? "no connection"));
      }

      Console.WriteLine($"[SKIP] PrincipalRoleStore_.Ef: {availability.SkipReason ?? "no connection"}");
      return false;
    }
  }

  public static IPrincipalRoleStore CreateStore()
  {
    PostgresAvailability availability = Availability.Value.GetAwaiter().GetResult();
    if (availability.AdminConnectionString is null)
    {
      throw new InvalidOperationException(
        availability.SkipReason ?? "Postgres is not available for EfPrincipalRoleStore tests.");
    }

    string databaseName = "ef_principal_roles_" + Guid.NewGuid().ToString("N");
    CreateDatabase(availability.AdminConnectionString, databaseName);
    string connectionString = new NpgsqlConnectionStringBuilder(availability.AdminConnectionString)
    {
      Database = databaseName
    }.ConnectionString;

    DbContextOptions<PostgresDbContext> options = new DbContextOptionsBuilder<PostgresDbContext>()
      .UseNpgsql(connectionString)
      .Options;
    PostgresDbContext db = new(options);
    db.Database.Migrate();
    return new EfPrincipalRoleStore(db);
  }

  /// <summary>New store instance on a fresh DbContext against the same database (simulates new scope).</summary>
  public static IPrincipalRoleStore CreateStoreOnSameDatabase(PostgresDbContext template)
  {
    string connectionString = template.Database.GetConnectionString()
      ?? throw new InvalidOperationException("Template context has no connection string.");
    DbContextOptions<PostgresDbContext> options = new DbContextOptionsBuilder<PostgresDbContext>()
      .UseNpgsql(connectionString)
      .Options;
    return new EfPrincipalRoleStore(new PostgresDbContext(options));
  }

  public static PostgresDbContext CreateDbContext()
  {
    PostgresAvailability availability = Availability.Value.GetAwaiter().GetResult();
    if (availability.AdminConnectionString is null)
    {
      throw new InvalidOperationException(availability.SkipReason ?? "no connection");
    }

    string databaseName = "ef_principal_roles_" + Guid.NewGuid().ToString("N");
    CreateDatabase(availability.AdminConnectionString, databaseName);
    string connectionString = new NpgsqlConnectionStringBuilder(availability.AdminConnectionString)
    {
      Database = databaseName
    }.ConnectionString;

    DbContextOptions<PostgresDbContext> options = new DbContextOptionsBuilder<PostgresDbContext>()
      .UseNpgsql(connectionString)
      .Options;
    PostgresDbContext db = new(options);
    db.Database.Migrate();
    return db;
  }

  private static void CreateDatabase(string adminConnectionString, string databaseName)
  {
    if (databaseName.Length != 51
        || !databaseName.StartsWith("ef_principal_roles_", StringComparison.Ordinal)
        || !databaseName.AsSpan(19).ToString().All(static c => char.IsAsciiHexDigitLower(c)))
    {
      throw new ArgumentException("Database name must be the CreateStore-minted form.", nameof(databaseName));
    }

    using NpgsqlConnection connection = new(adminConnectionString);
    connection.Open();
    using NpgsqlCommand command = connection.CreateCommand();
#pragma warning disable CA2100
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
      return new PostgresAvailability(builder.ConnectionString, SkipReason: null);
    }

    try
    {
      PostgreSqlContainer container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("timewarp_role_store_tests")
        .WithUsername("timewarp")
        .WithPassword("timewarp")
        .Build();

      await container.StartAsync();
      var builder = new NpgsqlConnectionStringBuilder(container.GetConnectionString())
      {
        Database = "postgres"
      };
      return new PostgresAvailability(builder.ConnectionString, SkipReason: null);
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
      return new PostgresAvailability(AdminConnectionString: null, skipReason);
    }
  }

  private sealed record PostgresAvailability(string? AdminConnectionString, string? SkipReason);
}

public class Principal_role_store
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Principal_role_store>();

  public static async Task Set_and_get_round_trips_across_contexts()
  {
    if (!EfPrincipalRoleStoreFactory.IsAvailable) return;

    await using PostgresDbContext db = EfPrincipalRoleStoreFactory.CreateDbContext();
    IPrincipalRoleStore write = new EfPrincipalRoleStore(db);
    PrincipalId id = PrincipalId.New();

    await write.SetRoleIdsAsync(id, [RoleIds.Administrator, RoleIds.Developer]);

    // New context = new DI scope after restart
    string cs = db.Database.GetConnectionString()!;
    await using PostgresDbContext db2 = new(
      new DbContextOptionsBuilder<PostgresDbContext>().UseNpgsql(cs).Options);
    IPrincipalRoleStore read = new EfPrincipalRoleStore(db2);

    IReadOnlyList<Guid> roles = await read.GetRoleIdsAsync(id);
    roles.Order().ShouldBe(
      new[] { RoleIds.Administrator, RoleIds.Developer }.Order());
  }

  public static async Task Empty_set_clears_stored_roles()
  {
    if (!EfPrincipalRoleStoreFactory.IsAvailable) return;

    IPrincipalRoleStore store = EfPrincipalRoleStoreFactory.CreateStore();
    PrincipalId id = PrincipalId.New();

    await store.SetRoleIdsAsync(id, [RoleIds.Member, RoleIds.Operator]);
    await store.SetRoleIdsAsync(id, []);

    IReadOnlyList<Guid> roles = await store.GetRoleIdsAsync(id);
    roles.ShouldBeEmpty();
  }

  public static async Task TryClaimFirstAdministrator_first_wins_second_stays_unassigned()
  {
    if (!EfPrincipalRoleStoreFactory.IsAvailable) return;

    await using PostgresDbContext db = EfPrincipalRoleStoreFactory.CreateDbContext();
    IPrincipalRoleStore firstStore = new EfPrincipalRoleStore(db);
    PrincipalId first = PrincipalId.New();
    PrincipalId second = PrincipalId.New();

    (await firstStore.TryClaimFirstAdministratorAsync(first)).ShouldBeTrue();

    string cs = db.Database.GetConnectionString()!;
    await using PostgresDbContext db2 = new(
      new DbContextOptionsBuilder<PostgresDbContext>().UseNpgsql(cs).Options);
    IPrincipalRoleStore secondStore = new EfPrincipalRoleStore(db2);

    (await secondStore.TryClaimFirstAdministratorAsync(second)).ShouldBeFalse();
    (await secondStore.GetRoleIdsAsync(second)).ShouldBeEmpty();
    IReadOnlyList<Guid> firstRoles = await secondStore.GetRoleIdsAsync(first);
    firstRoles.Order().ShouldBe(
      new[] { RoleIds.Administrator, RoleIds.Member }.Order());
  }

  public static async Task Missing_principal_returns_empty()
  {
    if (!EfPrincipalRoleStoreFactory.IsAvailable) return;

    IPrincipalRoleStore store = EfPrincipalRoleStoreFactory.CreateStore();
    IReadOnlyList<Guid> roles = await store.GetRoleIdsAsync(PrincipalId.New());
    roles.ShouldBeEmpty();
  }

  public static async Task Set_dedupes_role_ids()
  {
    if (!EfPrincipalRoleStoreFactory.IsAvailable) return;

    IPrincipalRoleStore store = EfPrincipalRoleStoreFactory.CreateStore();
    PrincipalId id = PrincipalId.New();

    await store.SetRoleIdsAsync(id, [RoleIds.Developer, RoleIds.Developer, RoleIds.Member]);
    IReadOnlyList<Guid> roles = await store.GetRoleIdsAsync(id);
    roles.Count.ShouldBe(2);
    roles.ShouldContain(RoleIds.Developer);
    roles.ShouldContain(RoleIds.Member);
  }
}
