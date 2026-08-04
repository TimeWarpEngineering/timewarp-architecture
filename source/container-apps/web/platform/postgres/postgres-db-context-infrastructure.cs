#region Purpose
// Host PostgreSQL EF context: template seam that maps product aggregates and inherits the aggregate SaveChanges hook.
#endregion

#region Design
// Postgres plumbing (module, health and environment checks, schema-creation hosted service) ships with
// the postgres flag; the model is product-owned. Profile is the teaching IAggregateRoot — DbSet +
// IEntityTypeConfiguration discovered via ApplyConfigurationsFromAssembly (feature files ending in
// -infrastructure.cs compile into this assembly). Identity Principal/Credential are also mapped
// here (schema "identity") as the first port-backed durable consumer (task 104-032); they are
// NOT IAggregateRoot — store-CAS lives in EfPrincipalStore, not AggregateDbContext's Version hook.
// PrincipalRoleAssignment (identity.principal_roles) is the durable IPrincipalRoleStore backend
// (task 147-006).
// Connection setup lives in PostgresDbModule.ConfigurePostgresDb, not OnConfiguring, so the
// context stays configuration-agnostic.
// Aggregate enforcement (DomainInvariantsGuard, EntityVersion.Next, child→root resolution,
// Version PropertyAccessMode pin) lives in AggregateDbContext (TimeWarp.Foundation.Persistence) and
// only applies to IAggregateRoot types — Principal/Credential intentionally skip it.
// Overrides of OnModelCreating should still call base.OnModelCreating (EF convention hygiene),
// but the Version convention no longer depends on it: AggregateDbContext's ConfigureConventions is
// sealed and always registers AggregateVersionConvention, a model-finalizing convention that
// runs after OnModelCreating regardless of override order (task 121).
// This host customizes conventions via OnConfigureConventions (AggregateDbContext's virtual hook) —
// ConfigureConventions itself is sealed on AggregateDbContext and always registers the aggregate Version
// convention first; hosts cannot reach it to skip that registration. OnConfigureConventions here
// registers generated TypedId ValueConverters (ConfigureTypedIdConventions) so [TypedId] properties
// map to Guid without per-entity ceremony; Profile and identity configs also set conversion
// explicitly as exemplars. Its namespace arrives via an MSBuild <Using> in the csproj, NOT a using
// directive here: the literal would be sourceName-rewritten on dotnet-new while the generator's
// baked-in namespace is not (task 115 pattern).
// Concurrency is now a one-party contract for IAggregateRoot: AggregateVersionConvention
// configures IsConcurrencyToken + PropertyAccessMode.Property for every mapped root's Version
// automatically — ProfileEntityTypeConfiguration no longer calls .IsConcurrencyToken() itself.
// Port-backed identity entities (Principal/Credential) are not IAggregateRoot, so the convention
// skips them; they keep store-CAS + a manual .IsConcurrencyToken() without the aggregate auto-increment.
#endregion

namespace TimeWarp.Architecture.Persistence;

using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Profiles.Domain;
using TimeWarp.Foundation.Persistence;
using TimeWarp.Identity;

public sealed partial class PostgresDbContext : AggregateDbContext
{
  public PostgresDbContext(DbContextOptions<PostgresDbContext> options) : base(options) { }

  public DbSet<Profile> Profiles => Set<Profile>();
  public DbSet<Principal> Principals => Set<Principal>();
  public DbSet<Credential> Credentials => Set<Credential>();
  public DbSet<PrincipalRoleAssignment> PrincipalRoleAssignments => Set<PrincipalRoleAssignment>();

  protected override void OnConfigureConventions(ModelConfigurationBuilder configurationBuilder)
  {
    configurationBuilder.ConfigureTypedIdConventions();
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(PostgresDbContext).Assembly);
  }
}
