#region Purpose
// EF Core mapping for the Role aggregate: identity.roles plus its two owned grant tables.
#endregion

#region Design
// Task 473, following profile-entity-type-configuration-infrastructure.cs. Schema "identity" next
// to principals and principal_roles (literal, no cross-slice reference). Table "roles": TypedId key
// stored as Guid, Name and Description required text (the contracts cap neither, so the columns do
// not either). IsProductRole is derived from RoleCatalog and not mapped.
// Children are OwnsMany onto the tables that already existed before the aggregate:
// identity.role_permissions (RoleId, PermissionId), max length 128, and identity.role_agent_scopes
// (RoleId, Scope), max length 64. Same column names, lengths, and composite keys, no surrogate key,
// so the AddRoles migration moves no data; the owner key column RoleId becomes a foreign key to
// identity.roles (cascade, so deleting a custom role deletes its rows). Field access on
// PermissionList / AgentScopeList, the private backing lists the read-only navigations expose.
// identity.principal_roles gets no foreign key: its RoleId is a raw Guid owned by the principals
// slice, SetPrincipalRoles accepts only RoleIds.All, and product roles cannot be deleted, so a
// principal_roles row cannot point at a deleted role through the API. Recorded on the task.
// Version: IsConcurrencyToken comes from AggregateVersionConvention; the explicit
// UsePropertyAccessMode pin is the Profile exemplar's local copy.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Infrastructure;

using TimeWarp.Architecture.Features.Admin.Roles.Domain;

public sealed class RoleEntityTypeConfiguration : IEntityTypeConfiguration<Role>
{
  public const string SchemaName = "identity";
  public const string TableName = "roles";
  public const string PermissionsTableName = "role_permissions";
  public const string AgentScopesTableName = "role_agent_scopes";
  private const string RoleIdColumn = "RoleId";

  public void Configure(EntityTypeBuilder<Role> builder)
  {
    builder.ToTable(TableName, SchemaName);

    builder.HasKey(role => role.Id);

    builder.Property(role => role.Id)
      .HasConversion
      (
        id => id.Value,
        value => RoleId.From(value)
      )
      .ValueGeneratedNever();

    builder.Property(role => role.Name).IsRequired();
    builder.Property(role => role.Description).IsRequired();
    builder.Ignore(role => role.IsProductRole);

    builder.OwnsMany(role => role.Permissions, permission =>
    {
      permission.ToTable(PermissionsTableName, SchemaName);
      permission.WithOwner().HasForeignKey(RoleIdColumn);
      permission.Property<RoleId>(RoleIdColumn)
        .HasConversion(
          id => id.Value,
          value => RoleId.From(value));
      permission.HasKey(RoleIdColumn, nameof(PermissionGrant.PermissionId));
      permission.Property(entry => entry.PermissionId)
        .HasMaxLength(PermissionGrant.MaxLength)
        .IsRequired();
    });
    builder.Navigation(role => role.Permissions)
      .HasField("PermissionList")
      .UsePropertyAccessMode(PropertyAccessMode.Field);

    builder.OwnsMany(role => role.AgentScopes, scope =>
    {
      scope.ToTable(AgentScopesTableName, SchemaName);
      scope.WithOwner().HasForeignKey(RoleIdColumn);
      scope.Property<RoleId>(RoleIdColumn)
        .HasConversion(
          id => id.Value,
          value => RoleId.From(value));
      scope.HasKey(RoleIdColumn, nameof(AgentScopeGrant.Scope));
      scope.Property(entry => entry.Scope)
        .HasMaxLength(AgentScopeGrant.MaxLength)
        .IsRequired();
    });
    builder.Navigation(role => role.AgentScopes)
      .HasField("AgentScopeList")
      .UsePropertyAccessMode(PropertyAccessMode.Field);

    builder.Property(role => role.Version)
      .UsePropertyAccessMode(PropertyAccessMode.Property);
  }
}
