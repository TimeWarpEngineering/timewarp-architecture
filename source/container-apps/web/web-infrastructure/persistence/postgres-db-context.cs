#region Purpose
// EF Core context for the PostgreSQL store; the seam where template consumers add their entity sets.
#endregion

#region Design
// Deliberately entity-free: the template ships the Postgres plumbing (module, health and
// environment checks, schema-creation hosted service) and leaves the model to the consumer.
// Connection setup lives in PostgresDbModule.ConfigurePostgresDb, not OnConfiguring, so the
// context stays configuration-agnostic.
#endregion

namespace TimeWarp.Architecture.Persistence;

public sealed partial class PostgresDbContext : DbContext { }
