#region Purpose
// EF Core mapping for PrincipalRoleAssignment → identity.principal_roles.
#endregion

#region Design
// Schema "identity" next to principals/credentials (147-006). Literal "identity" (not a
// reference to PrincipalEntityTypeConfiguration) — TWA0009 forbids Admin.Principals →
// Identity.Infrastructure slice coupling. Composite key; PrincipalId Guid conversion matches
// principal mapping. Discovered via ApplyConfigurationsFromAssembly on PostgresDbContext.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeWarp.Architecture.Features;
using TimeWarp.Identity;

public sealed class PrincipalRoleAssignmentEntityTypeConfiguration
  : IEntityTypeConfiguration<PrincipalRoleAssignment>
{
  public const string SchemaName = "identity";
  public const string TableName = "principal_roles";

  public void Configure(EntityTypeBuilder<PrincipalRoleAssignment> builder)
  {
    builder.ToTable(TableName, SchemaName);

    builder.HasKey(row => new { row.PrincipalId, row.RoleId });

    builder.Property(row => row.PrincipalId)
      .HasConversion(
        id => id.Value,
        value => PrincipalId.From(value))
      .IsRequired();

    builder.Property(row => row.RoleId).IsRequired();
  }
}
