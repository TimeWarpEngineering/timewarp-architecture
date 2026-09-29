#region Purpose
// Bounded Npgsql retrying execution strategy shared by the runtime PostgresDbContext registration and its tests.
#endregion

#region Design
// Task 255: `dev db reset` (or any Postgres restart/failover) kills every open backend with 57P01;
// the Npgsql pool then hands a dead connection to the next request. Without a retrying strategy
// EF classifies the failure as transient but throws. EnableRetryOnFailure replays the operation
// on a fresh connection instead.
// Bounds are explicit and modest: MaxRetryCount = 4, MaxRetryDelay = 2s (EF's exponential backoff
// with jitter, capped). Worst case a request waits a few seconds before surfacing the failure —
// long enough to ride out a dropped pool or brief restart, short enough not to mask an outage.
// No extra error codes: Npgsql's own transient classification (NpgsqlException.IsTransient —
// socket/IO failures, 57P01-style admin shutdown, 40001 serialization failure, 40P01 deadlock) is
// the retry set. DbUpdateConcurrencyException is NOT retried: it is raised by EF from a
// rows-affected mismatch, carries no transient NpgsqlException, so the stores' store-CAS
// translation and the callers' own optimistic-concurrency loops still see it on first attempt.
// Consequence for callers: a user-initiated transaction must run inside
// Database.CreateExecutionStrategy().ExecuteAsync (EF rejects it otherwise) and its body must be
// replayable (re-read inside the transaction). Single SaveChanges units need nothing — EF wraps
// them in the strategy itself.
// Lives in infrastructure (not PostgresDbModule) so web-infrastructure-tests build contexts with
// the exact production policy. The design-time factory deliberately does NOT use it: `dotnet ef`
// and the AppHost migration resource are one-shot runs against a database that is either reachable
// or not — no long-lived pool of connections to go stale — so a failure should surface immediately
// rather than back off silently.
#endregion

namespace TimeWarp.Architecture.Persistence;

using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

/// <summary>Runtime Npgsql retry policy for <see cref="PostgresDbContext"/>.</summary>
public static class PostgresRetryPolicy
{
  public const int MaxRetryCount = 4;

  public static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(2);

  /// <summary>Applies the bounded retrying execution strategy to an Npgsql options builder.</summary>
  public static void Configure(NpgsqlDbContextOptionsBuilder npgsqlDbContextOptionsBuilder)
  {
    ArgumentNullException.ThrowIfNull(npgsqlDbContextOptionsBuilder);
    npgsqlDbContextOptionsBuilder.EnableRetryOnFailure(MaxRetryCount, MaxRetryDelay, errorCodesToAdd: null);
  }
}
