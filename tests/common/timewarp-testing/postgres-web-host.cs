#region Purpose
// Builds an in-proc Web.Server host graph over a fresh, migrated Postgres database.
#endregion

#region Design
// Task 522 removed the in-memory feedback stores. A host with no Postgres connection keeps
// UnavailableFeedbackStorage, which refuses every filing and upload, so a test that files feedback
// (over HTTP or through the browser) builds its host here. Each call creates its own database:
// CREATE DATABASE with a Guid name, the committed migrations, then
// PostgresDbOptions:ConnectionString on the web host, the key a non-Aspire consumer sets.
// PostgresDbModule then registers the EF stores for principals, roles, profiles, and feedback.
// The host is the same Web-only graph as HostGraphFactory.CreateWebAsync (C-create: the caller
// disposes it). Postgres comes from PostgresTestAvailability (env connection string or
// Testcontainers, one container per process). With none, CreateWebAsync throws: these tests
// cannot run without a database, and under CI that is a failure, not a skip.
// The database's connection string turns pooling off. A pool is kept per connection string for
// the life of the process, so with pooling every disposed host would leave idle connections to its
// own database open, and a Playwright run of a few such hosts exhausts Postgres's max_connections
// (53300). Template-excluded when postgres or web is off.
#endregion

namespace TimeWarp.Architecture.Testing;

using Microsoft.EntityFrameworkCore;
using Npgsql;
using TimeWarp.Architecture.Persistence;

/// <summary>Web.Server host graphs whose stores run on their own Postgres database.</summary>
public static class PostgresWebHost
{
  private static readonly Lazy<Task<PostgresTestAvailability>> Availability =
    new(() => PostgresTestAvailability.ResolveAsync("timewarp_web_host_tests"), LazyThreadSafetyMode.ExecutionAndPublication);

  /// <summary>Same graph as <see cref="HostGraphFactory.CreateWebAsync"/>, over a new migrated database.</summary>
  public static async Task<HostGraph> CreateWebAsync(Action<IServiceCollection>? configureWeb = null)
  {
    Dictionary<string, string?> configuration = new(WebTestServerApplication.HttpsServiceEndpoints)
    {
      ["PostgresDbOptions:ConnectionString"] = await CreateDatabaseAsync().ConfigureAwait(false),
    };
    return await HostGraphFactory.CreateWebAsync(configureWeb, configuration).ConfigureAwait(false);
  }

  private static async Task<string> CreateDatabaseAsync()
  {
    PostgresTestAvailability availability = await Availability.Value.ConfigureAwait(false);
    string admin = availability.AdminConnectionString
      ?? throw new InvalidOperationException(
        "This test needs Postgres (task 522: there is no in-memory feedback store). " +
        (availability.SkipReason ?? "No connection."));
    string name = "web_host_" + Guid.NewGuid().ToString("N");
    await using (PostgresDbContext adminDb = Context(admin))
    {
      // CREATE DATABASE takes an identifier, which cannot be a parameter; the name is a Guid minted here.
      string createDatabase = $"CREATE DATABASE \"{name}\"";
      await adminDb.Database.ExecuteSqlRawAsync(createDatabase).ConfigureAwait(false);
    }

    string connectionString = new NpgsqlConnectionStringBuilder(admin) { Database = name, Pooling = false }.ConnectionString;
    await using PostgresDbContext db = Context(connectionString);
    await db.Database.MigrateAsync().ConfigureAwait(false);
    return connectionString;
  }

  private static PostgresDbContext Context(string connectionString) =>
    new(new DbContextOptionsBuilder<PostgresDbContext>().UseNpgsql(connectionString, PostgresRetryPolicy.Configure).Options);
}
