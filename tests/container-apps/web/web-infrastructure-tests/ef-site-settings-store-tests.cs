#region Purpose
// EF ISiteSettingsStore get/update/concurrency against ephemeral Postgres.
#endregion

namespace SiteSettingsStore_.Ef;

using Npgsql;
using TimeWarp.Architecture.Features.Settings.Infrastructure;
using TimeWarp.Architecture.Persistence;
using TimeWarp.Architecture.Testing;
using TimeWarp.Foundation.Entities;
using TimeWarp.Identity;

file sealed class EfSiteSettingsStoreFactory
{
  private static readonly Lazy<Task<PostgresTestAvailability>> Availability =
    new(() => PostgresTestAvailability.ResolveAsync("timewarp_site_settings_store_tests"), LazyThreadSafetyMode.ExecutionAndPublication);

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
          "EfSiteSettingsStore tests require a connection string or Docker under CI. " +
          (availability.SkipReason ?? "no connection"));
      }

      Console.WriteLine($"[SKIP] SiteSettingsStore_.Ef: {availability.SkipReason ?? "no connection"}");
      return false;
    }
  }

  public static ISiteSettingsStore CreateStore()
  {
    PostgresTestAvailability availability = Availability.Value.GetAwaiter().GetResult();
    if (availability.AdminConnectionString is null)
    {
      throw new InvalidOperationException(
        availability.SkipReason ?? "Postgres is not available for EfSiteSettingsStore tests.");
    }

    string databaseName = "ef_settings_" + Guid.NewGuid().ToString("N");
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
    return new EfSiteSettingsStore(db);
  }

  private static void CreateDatabase(string adminConnectionString, string databaseName)
  {
    if (databaseName.Length != 44
        || !databaseName.StartsWith("ef_settings_", StringComparison.Ordinal)
        || !databaseName.AsSpan(12).ToString().All(static c => char.IsAsciiHexDigitLower(c)))
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
}

public class Round_Trip
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Round_Trip>();

  public static async Task Add_get_update_concurrency()
  {
    if (!EfSiteSettingsStoreFactory.IsAvailable)
    {
      return;
    }

    ISiteSettingsStore store = EfSiteSettingsStoreFactory.CreateStore();
    await store.AddAsync(
      SiteSettings.Create(true, true, PasskeyPromptMode.Soft));

    SiteSettings? found = await store.GetAsync();
    found.ShouldNotBeNull();
    found!.EntraSignInEnabled.ShouldBeTrue();
    found.Version.ShouldBe(0);

    SiteSettings? a = await store.GetAsync();
    SiteSettings? b = await store.GetAsync();
    a!.ReplacePolicy(false, false, PasskeyPromptMode.Required);
    await store.UpdateAsync(a);
    SiteSettings? after = await store.GetAsync();
    after!.Version.ShouldBe(EntityVersion.Next(0));
    after.PasskeyPromptMode.ShouldBe(PasskeyPromptMode.Required);

    b!.ReplacePolicy(true, true, PasskeyPromptMode.Soft);
    await Should.ThrowAsync<ConcurrencyConflictException>(() => store.UpdateAsync(b));
  }
}
