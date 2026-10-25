#region Purpose
// The accepted-tenant migration turns a stored GUID array into entries with no domain.
#endregion

#region Design
// Migrate to the migration before ConvertEntraAcceptedTenants, insert the old jsonb shape
// with raw SQL, then migrate forward. The current model is not queried until the new column
// exists. Sign-in acceptance still matches the tid. No live Microsoft call.
#endregion

namespace EntraAcceptedTenantsMigration_.Ef;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using TimeWarp.Architecture.Testing;
using TimeWarp.Identity;

file sealed class MigrationDatabase
{
  private static readonly Lazy<Task<PostgresTestAvailability>> Availability =
    new(() => PostgresTestAvailability.ResolveAsync("timewarp_entra_tenant_migration_tests"), LazyThreadSafetyMode.ExecutionAndPublication);

  public static bool IsAvailable
  {
    get
    {
      PostgresTestAvailability availability = Availability.Value.GetAwaiter().GetResult();
      if (availability.AdminConnectionString is not null)
      {
        return true;
      }

      if (PostgresTestAvailability.IsCiEnvironment())
      {
        throw new InvalidOperationException(
          "Entra accepted-tenant migration tests require a connection string or Docker under CI. " +
          (availability.SkipReason ?? "no connection"));
      }

      Console.WriteLine($"[SKIP] EntraAcceptedTenantsMigration_.Ef: {availability.SkipReason ?? "no connection"}");
      return false;
    }
  }

  public static PostgresDbContext CreateEmpty()
  {
    PostgresTestAvailability availability = Availability.Value.GetAwaiter().GetResult();
    string adminConnectionString = availability.AdminConnectionString
      ?? throw new InvalidOperationException(availability.SkipReason ?? "Postgres is not available.");

    string databaseName = "ef_entra_" + Guid.NewGuid().ToString("N");
    if (databaseName.Length != 41
        || !databaseName.StartsWith("ef_entra_", StringComparison.Ordinal)
        || !databaseName.AsSpan(9).ToString().All(static c => char.IsAsciiHexDigitLower(c)))
    {
      throw new ArgumentException("Database name must be the minted form.", nameof(databaseName));
    }

    using (NpgsqlConnection connection = new(adminConnectionString))
    {
      connection.Open();
      using NpgsqlCommand command = connection.CreateCommand();
#pragma warning disable CA2100 // databaseName is minted above from a Guid, never caller input.
      command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
#pragma warning restore CA2100
      command.ExecuteNonQuery();
    }

    string connectionString = new NpgsqlConnectionStringBuilder(adminConnectionString)
    {
      Database = databaseName
    }.ConnectionString;
    DbContextOptions<PostgresDbContext> options = new DbContextOptionsBuilder<PostgresDbContext>()
      .UseNpgsql(connectionString, PostgresRetryPolicy.Configure)
      .Options;
    return new PostgresDbContext(options);
  }
}

public class Convert_Given_
{
  private static readonly Guid First = Guid.Parse("00000000-0000-0000-0000-000000000001");
  private static readonly Guid Second = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Convert_Given_>();

  public static async Task Guid_Array_Should_Become_Entries_With_No_Domain()
  {
    if (!MigrationDatabase.IsAvailable)
    {
      return;
    }

    await using PostgresDbContext db = MigrationDatabase.CreateEmpty();
    string[] migrations = [.. db.Database.GetMigrations()];
    string target = migrations.Single(id => id.EndsWith("_ConvertEntraAcceptedTenants", StringComparison.Ordinal));
    int index = Array.IndexOf(migrations, target);
    string previous = migrations[index - 1];
    await db.GetService<IMigrator>().MigrateAsync(previous);

    // SingletonId is 6f1c2e90-2190-4006-8000-51ee77000001. The column is still the GUID array here.
    await db.Database.ExecuteSqlRawAsync(
      """
      INSERT INTO identity.site_settings (
        "Id",
        "AssistantName",
        "EntraAcceptedTenantIds",
        "EntraAllowBootstrap",
        "EntraSignInEnabled",
        "EntraTenantAcceptanceInitialized",
        "EntraTenantAcceptanceMode",
        "PasskeyPromptMode",
        "Version")
      VALUES (
        '6f1c2e90-2190-4006-8000-51ee77000001',
        'Zawadi',
        '["00000000-0000-0000-0000-000000000001","aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"]'::jsonb,
        false,
        true,
        true,
        0,
        0,
        0);
      """);

    await db.GetService<IMigrator>().MigrateAsync(target);

    SiteSettings settings = await db.Set<SiteSettings>().SingleAsync();
    settings.EntraAcceptedTenants.Count.ShouldBe(2);
    settings.EntraAcceptedTenants.ShouldAllBe(tenant => tenant.Domain == null);
    settings.EntraAcceptedTenants.Select(tenant => tenant.TenantId).ShouldBe([First, Second], ignoreOrder: true);
    settings.AcceptsEntraTenant(First).ShouldBeTrue();
    settings.AcceptsEntraTenant(Second).ShouldBeTrue();
    settings.AcceptsEntraTenant(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")).ShouldBeFalse();
  }
}
