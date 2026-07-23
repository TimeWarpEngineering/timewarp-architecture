#region Purpose
// Host PostgreSQL EF context: template seam that maps product aggregates and inherits the golden SaveChanges hook.
#endregion

#region Design
// Postgres plumbing (module, health and environment checks, schema-creation hosted service) ships with
// the postgres flag; the model is product-owned. Profile is the teaching aggregate — DbSet +
// IEntityTypeConfiguration discovered via ApplyConfigurationsFromAssembly (feature files ending in
// -infrastructure.cs compile into this assembly).
// Connection setup lives in PostgresDbModule.ConfigurePostgresDb, not OnConfiguring, so the
// context stays configuration-agnostic.
// Golden aggregate enforcement (DomainInvariantsGuard, EntityVersion.Next, child→root resolution,
// Version PropertyAccessMode pin) lives in GoldenDbContext (TimeWarp.Foundation.Persistence).
// Overrides of OnModelCreating must call base.OnModelCreating so the golden pin still runs.
// ConfigureConventions registers generated TypedId ValueConverters (ConfigureTypedIdConventions)
// so [TypedId] properties map to Guid without per-entity ceremony; Profile also sets conversion
// explicitly as the exemplar. Its namespace arrives via an MSBuild <Using> in the csproj, NOT a
// using directive here: the literal would be sourceName-rewritten on dotnet-new while the
// generator's baked-in namespace is not (task 115 pattern).
// Concurrency is a two-party contract: GoldenDbContext increments Version on Modified roots; the
// host must pair .IsConcurrencyToken() (ProfileEntityTypeConfiguration does) or the increment is
// silent bookkeeping with no WHERE check.
#endregion

namespace TimeWarp.Architecture.Persistence;

using TimeWarp.Architecture.Aggregates.Profiles;
using TimeWarp.Foundation.Persistence;

public sealed partial class PostgresDbContext : GoldenDbContext
{
  public PostgresDbContext(DbContextOptions<PostgresDbContext> options) : base(options) { }

  public DbSet<Profile> Profiles => Set<Profile>();

  protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
  {
    configurationBuilder.ConfigureTypedIdConventions();
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(PostgresDbContext).Assembly);
  }
}
