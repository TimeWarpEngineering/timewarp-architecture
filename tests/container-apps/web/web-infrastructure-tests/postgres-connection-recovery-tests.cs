#region Purpose
// PostgresRetryPolicy recovers from backends killed under a live context (task 255) — pool and mid-transaction.
#endregion

#region Design
// Reproduces `dev db reset` while web-server runs: pg_terminate_backend kills the context's pooled
// connection between two queries (Postgres sends 57P01). Each test mints its own database so the
// terminate only reaches this test's backends and its pool key is private. A control context
// WITHOUT the policy proves the kill really reaches the second query (otherwise the recovery
// assertion would be vacuous). The claim test terminates its own backend from inside the
// serializable transaction via a command interceptor, proving the execution strategy replays the
// whole unit and still commits.
#endregion

namespace PostgresConnectionRecovery_;

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Admin.Principals.Infrastructure;
using TimeWarp.Architecture.Testing;
using TimeWarp.Identity;

file static class RecoveryDatabase
{
  private static readonly Lazy<Task<PostgresTestAvailability>> Availability =
    new(() => PostgresTestAvailability.ResolveAsync("timewarp_connection_recovery_tests"), LazyThreadSafetyMode.ExecutionAndPublication);

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
          "Postgres connection recovery tests require a connection string or Docker under CI. " +
          (availability.SkipReason ?? "no connection"));
      }

      Console.WriteLine($"[SKIP] PostgresConnectionRecovery_: {availability.SkipReason ?? "no connection"}");
      return false;
    }
  }

  public static string AdminConnectionString =>
    Availability.Value.GetAwaiter().GetResult().AdminConnectionString
    ?? throw new InvalidOperationException("Postgres is not available for connection recovery tests.");

  /// <summary>Creates and migrates a fresh database; returns its connection string.</summary>
  public static string CreateMigratedDatabase()
  {
    string databaseName = "ef_recovery_" + Guid.NewGuid().ToString("N");
    using (NpgsqlConnection connection = new(AdminConnectionString))
    {
      connection.Open();
      using NpgsqlCommand command = connection.CreateCommand();
#pragma warning disable CA2100 // databaseName is minted above from a Guid — no user input.
      command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
#pragma warning restore CA2100
      command.ExecuteNonQuery();
    }

    string connectionString = new NpgsqlConnectionStringBuilder(AdminConnectionString)
    {
      Database = databaseName
    }.ConnectionString;

    using PostgresDbContext db = CreateContext(connectionString, withRetry: true);
    db.Database.Migrate();
    return connectionString;
  }

  public static PostgresDbContext CreateContext(string connectionString, bool withRetry, params IInterceptor[] interceptors)
  {
    DbContextOptionsBuilder<PostgresDbContext> builder = new();
    if (withRetry)
    {
      builder.UseNpgsql(connectionString, PostgresRetryPolicy.Configure);
    }
    else
    {
      builder.UseNpgsql(connectionString);
    }

    if (interceptors.Length > 0)
    {
      builder.AddInterceptors(interceptors);
    }

    return new PostgresDbContext(builder.Options);
  }

  /// <summary>Terminates every backend connected to <paramref name="connectionString"/>'s database.</summary>
  public static async Task<int> TerminateBackendsAsync(string connectionString)
  {
    string databaseName = new NpgsqlConnectionStringBuilder(connectionString).Database
      ?? throw new InvalidOperationException("Connection string has no database.");
    await using NpgsqlConnection connection = new(AdminConnectionString);
    await connection.OpenAsync();
    await using NpgsqlCommand command = connection.CreateCommand();
    command.CommandText =
      "SELECT count(pg_terminate_backend(pid)) FROM pg_stat_activity " +
      "WHERE datname = @databaseName AND pid <> pg_backend_pid()";
    command.Parameters.AddWithValue("databaseName", databaseName);
    object? terminated = await command.ExecuteScalarAsync();
    return Convert.ToInt32(terminated, System.Globalization.CultureInfo.InvariantCulture);
  }

  public static async Task TerminateBackendAsync(int processId)
  {
    await using NpgsqlConnection connection = new(AdminConnectionString);
    await connection.OpenAsync();
    await using NpgsqlCommand command = connection.CreateCommand();
    command.CommandText = "SELECT pg_terminate_backend(@processId)";
    command.Parameters.AddWithValue("processId", processId);
    await command.ExecuteScalarAsync();
  }
}

/// <summary>Kills the executing command's own backend the first time a principal_roles read runs.</summary>
file sealed class TerminateOnceInterceptor : DbCommandInterceptor
{
  public int Terminations { get; private set; }

  public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
    DbCommand command,
    CommandEventData eventData,
    InterceptionResult<object> result,
    CancellationToken cancellationToken = default)
  {
    await TerminateFirstAsync(command).ConfigureAwait(false);
    return result;
  }

  public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
    DbCommand command,
    CommandEventData eventData,
    InterceptionResult<DbDataReader> result,
    CancellationToken cancellationToken = default)
  {
    await TerminateFirstAsync(command).ConfigureAwait(false);
    return result;
  }

  private async Task TerminateFirstAsync(DbCommand command)
  {
    if (Terminations > 0
        || command.Transaction is null
        || !command.CommandText.Contains("principal_roles", StringComparison.Ordinal)
        || command.Connection is not NpgsqlConnection connection)
    {
      return;
    }

    Terminations++;
    await RecoveryDatabase.TerminateBackendAsync(connection.ProcessID).ConfigureAwait(false);
    // Give the server a moment to deliver the termination before the command is written.
    await Task.Delay(200).ConfigureAwait(false);
  }
}

public class Connection_recovery
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Connection_recovery>();

  public static async Task Terminated_pooled_connection_without_retry_fails_second_query()
  {
    if (!RecoveryDatabase.IsAvailable) return;

    string connectionString = RecoveryDatabase.CreateMigratedDatabase();
    await using PostgresDbContext db = RecoveryDatabase.CreateContext(connectionString, withRetry: false);

    (await db.PrincipalRoleAssignments.CountAsync()).ShouldBe(0);
    (await RecoveryDatabase.TerminateBackendsAsync(connectionString)).ShouldBeGreaterThan(0);

    await Should.ThrowAsync<Exception>(async () => await db.PrincipalRoleAssignments.CountAsync());
  }

  public static async Task Terminated_pooled_connection_with_retry_policy_recovers_second_query()
  {
    if (!RecoveryDatabase.IsAvailable) return;

    string connectionString = RecoveryDatabase.CreateMigratedDatabase();
    await using PostgresDbContext db = RecoveryDatabase.CreateContext(connectionString, withRetry: true);
    IPrincipalRoleStore store = new EfPrincipalRoleStore(db);
    PrincipalId principalId = PrincipalId.New();
    await store.SetRoleIdsAsync(principalId, [RoleIds.Member]);

    (await store.GetRoleIdsAsync(principalId)).ShouldBe([RoleIds.Member]);
    (await RecoveryDatabase.TerminateBackendsAsync(connectionString)).ShouldBeGreaterThan(0);

    (await store.GetRoleIdsAsync(principalId)).ShouldBe([RoleIds.Member]);
  }

  public static async Task Claim_transaction_replays_after_backend_terminated_mid_transaction()
  {
    if (!RecoveryDatabase.IsAvailable) return;

    string connectionString = RecoveryDatabase.CreateMigratedDatabase();
    TerminateOnceInterceptor interceptor = new();
    await using PostgresDbContext db = RecoveryDatabase.CreateContext(connectionString, withRetry: true, interceptor);
    IPrincipalRoleStore store = new EfPrincipalRoleStore(db);
    PrincipalId principalId = PrincipalId.New();

    (await store.TryClaimFirstAdministratorAsync(principalId)).ShouldBeTrue();
    interceptor.Terminations.ShouldBe(1);

    await using PostgresDbContext verify = RecoveryDatabase.CreateContext(connectionString, withRetry: true);
    IReadOnlyList<Guid> roles = await new EfPrincipalRoleStore(verify).GetRoleIdsAsync(principalId);
    roles.Order().ShouldBe(new[] { RoleIds.Administrator, RoleIds.Member }.Order());
  }

  public static async Task Concurrent_claims_under_retry_policy_elect_exactly_one_administrator()
  {
    if (!RecoveryDatabase.IsAvailable) return;

    string connectionString = RecoveryDatabase.CreateMigratedDatabase();
    const int claimants = 4;
    PostgresDbContext[] contexts = Enumerable.Range(0, claimants)
      .Select(_ => RecoveryDatabase.CreateContext(connectionString, withRetry: true))
      .ToArray();
    try
    {
      PrincipalId[] principals = Enumerable.Range(0, claimants).Select(_ => PrincipalId.New()).ToArray();

      // Serialization failures (40001) are transient: losers replay, re-read, and return false
      // instead of throwing.
      bool[] results = await Task.WhenAll(
        contexts.Select((context, index) =>
          new EfPrincipalRoleStore(context).TryClaimFirstAdministratorAsync(principals[index])));

      results.Count(static won => won).ShouldBe(1);

      await using PostgresDbContext verify = RecoveryDatabase.CreateContext(connectionString, withRetry: true);
      int administrators = await verify.PrincipalRoleAssignments
        .CountAsync(row => row.RoleId == RoleIds.Administrator);
      administrators.ShouldBe(1);
    }
    finally
    {
      foreach (PostgresDbContext context in contexts)
      {
        await context.DisposeAsync();
      }
    }
  }
}
