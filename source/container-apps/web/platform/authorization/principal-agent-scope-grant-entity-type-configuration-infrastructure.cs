#region Purpose
// EF Core mapping for PrincipalAgentScopeGrant → identity.principal_agent_scopes.
#endregion

#region Design
// Task 466: schema "identity". PrincipalId Guid conversion matches principal_roles. Scope
// length matches the issuance contract's per-entry ceiling (64).
#endregion

namespace TimeWarp.Architecture.Authorization;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeWarp.Identity;

public sealed class PrincipalAgentScopeGrantEntityTypeConfiguration
  : IEntityTypeConfiguration<PrincipalAgentScopeGrant>
{
  public const string SchemaName = "identity";
  public const string TableName = "principal_agent_scopes";

  public void Configure(EntityTypeBuilder<PrincipalAgentScopeGrant> builder)
  {
    builder.ToTable(TableName, SchemaName);

    builder.HasKey(row => new { row.PrincipalId, row.Scope });

    builder.Property(row => row.PrincipalId)
      .HasConversion(
        id => id.Value,
        value => PrincipalId.From(value))
      .IsRequired();

    builder.Property(row => row.Scope)
      .IsRequired()
      .HasMaxLength(64);
  }
}
