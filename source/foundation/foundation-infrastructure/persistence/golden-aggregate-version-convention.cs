#region Purpose
// Model-finalizing convention: configures IsConcurrencyToken + PropertyAccessMode.Property on
// every mapped IAggregateRoot's Version property, with no per-host mapping required.
#endregion

#region Design
// Registered once by GoldenDbContext's sealed ConfigureConventions (see golden-db-context.cs) —
// this is the mechanism that turns the Version concurrency contract from two-party (host must
// remember .IsConcurrencyToken()) into one-party (GoldenDbContext supplies it unconditionally).
// IModelFinalizingConvention.ProcessModelFinalizing runs at model-finalizing time: strictly after
// OnModelCreating, ApplyConfigurationsFromAssembly, and every other convention/explicit mapping
// call, regardless of ordering. That closes the latent gap the previous OnModelCreating-loop
// implementation had — a config-only aggregate (an IEntityTypeConfiguration applied after
// base.OnModelCreating, with no DbSet property surfacing it earlier) could be added to the model
// after that loop already ran and would silently miss the pin. Model-finalizing time has no
// "before/after" for a given entity type; it always sees the complete model.
// Scope is IAggregateRoot only, matching GoldenDbContext.SaveChanges's own enforcement boundary.
// Store-CAS entities that are not IAggregateRoot (TimeWarp.Identity Principal/Credential,
// 104-032) keep their own manual .IsConcurrencyToken() in their entity configurations — this
// convention must never reach them, since GoldenDbContext does not auto-increment their Version.
// A missing "Version" property is not an error here: SaveChanges (IncrementModifiedVersions) is
// the fail-closed enforcement point for a misdeclared aggregate root, not this convention —
// finding no property to configure is simply a no-op.
#endregion

namespace TimeWarp.Foundation.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using TimeWarp.Foundation.Entities;

internal sealed class GoldenAggregateVersionConvention : IModelFinalizingConvention
{
  public void ProcessModelFinalizing
  (
    IConventionModelBuilder modelBuilder,
    IConventionContext<IConventionModelBuilder> context
  )
  {
    foreach (IConventionEntityType entityType in modelBuilder.Metadata.GetEntityTypes())
    {
      if (!typeof(IAggregateRoot).IsAssignableFrom(entityType.ClrType)) continue;

      IConventionProperty? version = entityType.FindProperty(GoldenDbContext.VersionPropertyName);
      if (version is null) continue;

      version.Builder.IsConcurrencyToken(true);
      version.Builder.UsePropertyAccessMode(PropertyAccessMode.Property);
    }
  }
}
