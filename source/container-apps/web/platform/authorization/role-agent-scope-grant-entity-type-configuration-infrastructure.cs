#region Purpose
// EF Core mapping for RoleAgentScopeGrant → identity.role_agent_scopes.
#endregion

#region Design
// Task 466: schema "identity" beside role_permissions. Composite key. Scope length matches the
// issuance contract's per-entry ceiling (64). Discovered via ApplyConfigurationsFromAssembly.
#endregion

namespace TimeWarp.Architecture.Authorization;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class RoleAgentScopeGrantEntityTypeConfiguration
  : IEntityTypeConfiguration<RoleAgentScopeGrant>
{
  public const string SchemaName = "identity";
  public const string TableName = "role_agent_scopes";

  public void Configure(EntityTypeBuilder<RoleAgentScopeGrant> builder)
  {
    builder.ToTable(TableName, SchemaName);

    builder.HasKey(row => new { row.RoleId, row.Scope });

    builder.Property(row => row.RoleId).IsRequired();

    builder.Property(row => row.Scope)
      .IsRequired()
      .HasMaxLength(64);
  }
}
