#region Purpose
// EF Core mapping for RolePermissionGrant → identity.role_permissions.
#endregion

#region Design
// Schema "identity" next to principal_roles (182-001). Literal "identity" — no cross-slice
// reference to PrincipalRoleAssignmentEntityTypeConfiguration (TWA0009). Composite key;
// PermissionId is required text (dotted string registry ids). Discovered via
// ApplyConfigurationsFromAssembly on PostgresDbContext.
#endregion

namespace TimeWarp.Architecture.Features.Authorization.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeWarp.Architecture.Features;

public sealed class RolePermissionGrantEntityTypeConfiguration
  : IEntityTypeConfiguration<RolePermissionGrant>
{
  public const string SchemaName = "identity";
  public const string TableName = "role_permissions";

  public void Configure(EntityTypeBuilder<RolePermissionGrant> builder)
  {
    builder.ToTable(TableName, SchemaName);

    builder.HasKey(row => new { row.RoleId, row.PermissionId });

    builder.Property(row => row.RoleId).IsRequired();

    builder.Property(row => row.PermissionId)
      .IsRequired()
      .HasMaxLength(128);
  }
}
