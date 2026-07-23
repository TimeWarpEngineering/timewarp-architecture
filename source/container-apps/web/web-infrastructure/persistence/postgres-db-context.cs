#region Purpose
// Host PostgreSQL EF context: entity-free template seam that inherits the golden aggregate SaveChanges hook.
#endregion

#region Design
// Deliberately entity-free: the template ships the Postgres plumbing (module, health and
// environment checks, schema-creation hosted service) and leaves the model to the consumer.
// Connection setup lives in PostgresDbModule.ConfigurePostgresDb, not OnConfiguring, so the
// context stays configuration-agnostic.
// Golden aggregate enforcement (DomainInvariantsGuard, EntityVersion.Next, child→root resolution,
// Version PropertyAccessMode pin) lives in GoldenDbContext (TimeWarp.Foundation.Persistence).
// This type is the postgres-flag host subclass only — add DbSets and host-side mappings here
// (including .IsConcurrencyToken() on Version for each concrete aggregate). Overrides of
// OnModelCreating must call base.OnModelCreating so the golden pin still runs.
// Concurrency is a two-party contract: GoldenDbContext increments Version on Modified roots; the
// host must pair .IsConcurrencyToken() or the increment is silent bookkeeping with no WHERE check.
#endregion

namespace TimeWarp.Architecture.Persistence;

using TimeWarp.Foundation.Persistence;

public sealed partial class PostgresDbContext : GoldenDbContext
{
  public PostgresDbContext(DbContextOptions<PostgresDbContext> options) : base(options) { }
}
