// EF fixture for the shared IPrincipalStore contract (task 104-032). Uses ephemeral Postgres
// (env connection string or Testcontainers). Each CreateStore gets an independent database so
// multi-store cases stay isolated. CI fails closed when neither Docker nor a connection string
// is available (same spirit as Profile live tests); interactive hosts soft-skip via ShouldSkip.

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

public class Principals : PrincipalStoreContract_.Principals
{
  protected override IPrincipalStoreFactory Factory => EfFixture.Factory;
  protected override bool ShouldSkip() => EfFixture.ShouldSkip();
}

public class Credentials : PrincipalStoreContract_.Credentials
{
  protected override IPrincipalStoreFactory Factory => EfFixture.Factory;
  protected override bool ShouldSkip() => EfFixture.ShouldSkip();
}

public class SnapshotSemantics : PrincipalStoreContract_.SnapshotSemantics
{
  protected override IPrincipalStoreFactory Factory => EfFixture.Factory;
  protected override bool ShouldSkip() => EfFixture.ShouldSkip();
}

public class AddPersistsVersionAsIs : PrincipalStoreContract_.AddPersistsVersionAsIs
{
  protected override IPrincipalStoreFactory Factory => EfFixture.Factory;
  protected override bool ShouldSkip() => EfFixture.ShouldSkip();
}

public class StalePrincipalUpdate : PrincipalStoreContract_.StalePrincipalUpdate
{
  protected override IPrincipalStoreFactory Factory => EfFixture.Factory;
  protected override bool ShouldSkip() => EfFixture.ShouldSkip();
}

public class CallerAheadConflict : PrincipalStoreContract_.CallerAheadConflict
{
  protected override IPrincipalStoreFactory Factory => EfFixture.Factory;
  protected override bool ShouldSkip() => EfFixture.ShouldSkip();
}

public class RevokeResurrectionRace : PrincipalStoreContract_.RevokeResurrectionRace
{
  protected override IPrincipalStoreFactory Factory => EfFixture.Factory;
  protected override bool ShouldSkip() => EfFixture.ShouldSkip();
}

public class QuarantineLossRace : PrincipalStoreContract_.QuarantineLossRace
{
  protected override IPrincipalStoreFactory Factory => EfFixture.Factory;
  protected override bool ShouldSkip() => EfFixture.ShouldSkip();
}

public class TierDemotionRace : PrincipalStoreContract_.TierDemotionRace
{
  protected override IPrincipalStoreFactory Factory => EfFixture.Factory;
  protected override bool ShouldSkip() => EfFixture.ShouldSkip();
}

public class AttachBumpsPrincipalVersion : PrincipalStoreContract_.AttachBumpsPrincipalVersion
{
  protected override IPrincipalStoreFactory Factory => EfFixture.Factory;
  protected override bool ShouldSkip() => EfFixture.ShouldSkip();
}

public class CallerInstanceNotAdvanced : PrincipalStoreContract_.CallerInstanceNotAdvanced
{
  protected override IPrincipalStoreFactory Factory => EfFixture.Factory;
  protected override bool ShouldSkip() => EfFixture.ShouldSkip();
}
