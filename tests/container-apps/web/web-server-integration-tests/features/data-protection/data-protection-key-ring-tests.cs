#region Purpose
// Records that two file-system key rings cannot unprotect each other's payload, and that two web-server processes on one Postgres database can.
#endregion

#region Design
// The file-system case is the cold-start failure: a new container directory has none of the
// previous process's keys, so an auth-cookie stand-in fails to unprotect. Both providers use
// DataProtectionDefaults.ApplicationName, so the failure is the missing key, not a different
// discriminator. The Postgres case migrates a fresh database, protects on one host, disposes
// that host, and unprotects on a second host. Template-excluded when postgres is off.
// Postgres comes from PostgresTestAvailability (env connection string or Testcontainers).
#endregion

namespace DataProtectionKeyRing_;

using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using TimeWarp.Architecture.Persistence;

[TestTag("DataProtection")]
public class KeyRing_Given_
{
  private const string Purpose = "TimeWarp.Architecture.DataProtection.Restart";
  private const string Payload = "auth-cookie-stand-in";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<KeyRing_Given_>();

  public static Task Separate_Key_Directories_Should_Fail_Unprotect()
  {
    DirectoryInfo first = Directory.CreateTempSubdirectory("dp-a-");
    DirectoryInfo second = Directory.CreateTempSubdirectory("dp-b-");
    try
    {
      string protectedPayload = ProtectToDirectory(first, Payload);
      Should.Throw<CryptographicException>(() => UnprotectFromDirectory(second, protectedPayload));
    }
    finally
    {
      first.Delete(recursive: true);
      second.Delete(recursive: true);
    }

    return Task.CompletedTask;
  }

  public static async Task Second_Web_Server_Should_Unprotect_What_The_First_Protected()
  {
    PostgresTestAvailability availability = await PostgresTestAvailability.ResolveAsync("timewarp_data_protection_key_ring_tests");
    if (availability.AdminConnectionString is not { } adminConnectionString)
    {
      if (PostgresTestAvailability.IsCiEnvironment())
      {
        throw new InvalidOperationException("Data protection key-ring regression requires Postgres under CI. " + availability.SkipReason);
      }

      Console.WriteLine($"[SKIP] DataProtectionKeyRing_: {availability.SkipReason ?? "no connection"}");
      return;
    }

    string connectionString = await CreateDatabaseAsync(adminConnectionString);
    await using (PostgresDbContext db = CreateContext(connectionString))
    {
      await db.Database.MigrateAsync();
    }

    Dictionary<string, string?> configuration = new(WebTestServerApplication.HttpsServiceEndpoints)
    {
      ["PostgresDbOptions:ConnectionString"] = connectionString,
    };

    string protectedPayload;
    await using (HostGraph first = await HostGraphFactory.CreateWebAsync(serviceEndpoints: configuration))
    {
      protectedPayload = Protect(first.Web!.WebApplicationHost.ServiceProvider, Payload);
    }

    await using HostGraph second = await HostGraphFactory.CreateWebAsync(serviceEndpoints: configuration);
    string plain = Unprotect(second.Web!.WebApplicationHost.ServiceProvider, protectedPayload);
    plain.ShouldBe(Payload);

    IOptions<DataProtectionOptions> options = second.Web.WebApplicationHost.ServiceProvider
      .GetRequiredService<IOptions<DataProtectionOptions>>();
    options.Value.ApplicationDiscriminator.ShouldBe(DataProtectionDefaults.ApplicationName);

    await using AsyncServiceScope scope = second.Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    PostgresDbContext keys = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
    int count = await keys.DataProtectionKeys.CountAsync();
    count.ShouldBeGreaterThan(0);
  }

  private static string ProtectToDirectory(DirectoryInfo directory, string payload)
  {
    ServiceCollection services = new();
    services.AddDataProtection().PersistKeysToFileSystem(directory).SetApplicationName(DataProtectionDefaults.ApplicationName);
    using ServiceProvider provider = services.BuildServiceProvider();
    return provider.GetRequiredService<IDataProtectionProvider>().CreateProtector(Purpose).Protect(payload);
  }

  private static string UnprotectFromDirectory(DirectoryInfo directory, string protectedPayload)
  {
    ServiceCollection services = new();
    services.AddDataProtection().PersistKeysToFileSystem(directory).SetApplicationName(DataProtectionDefaults.ApplicationName);
    using ServiceProvider provider = services.BuildServiceProvider();
    return provider.GetRequiredService<IDataProtectionProvider>().CreateProtector(Purpose).Unprotect(protectedPayload);
  }

  private static string Protect(IServiceProvider services, string payload) =>
    services.GetRequiredService<IDataProtectionProvider>().CreateProtector(Purpose).Protect(payload);

  private static string Unprotect(IServiceProvider services, string protectedPayload) =>
    services.GetRequiredService<IDataProtectionProvider>().CreateProtector(Purpose).Unprotect(protectedPayload);

  private static async Task<string> CreateDatabaseAsync(string adminConnectionString)
  {
    string databaseName = "dp_" + Guid.NewGuid().ToString("N");
    await using NpgsqlConnection connection = new(adminConnectionString);
    await connection.OpenAsync();
    await using NpgsqlCommand command = connection.CreateCommand();
#pragma warning disable CA2100 // databaseName is minted above from a Guid, never caller input.
    command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
#pragma warning restore CA2100
    await command.ExecuteNonQueryAsync();
    return new NpgsqlConnectionStringBuilder(adminConnectionString) { Database = databaseName }.ConnectionString;
  }

  private static PostgresDbContext CreateContext(string connectionString) =>
    new(new DbContextOptionsBuilder<PostgresDbContext>().UseNpgsql(connectionString).Options);
}
