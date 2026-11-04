// Connection-free EF model coverage for the Role aggregate (task 473): identity.roles, the two
// owned collections on the pre-existing grant tables (same keys, columns, lengths), field access.

namespace Role_Model_Mapping_;

using Microsoft.EntityFrameworkCore.Metadata;
using TimeWarp.Architecture.Features.Admin.Roles.Domain;
using TimeWarp.Architecture.Features.Admin.Roles.Infrastructure;

public class Map
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Map>();

  public static async Task Role_maps_to_identity_roles_with_typed_id_and_concurrency_token()
  {
    await using PostgresDbContext db = CreateModelOnlyContext();
    IEntityType role = db.Model.FindEntityType(typeof(Role)).ShouldNotBeNull("Role must be on the PostgresDbContext model");

    role.GetSchema().ShouldBe("identity");
    role.GetTableName().ShouldBe("roles");
    IProperty id = role.FindProperty(nameof(Role.Id)).ShouldNotBeNull();
    id.ClrType.ShouldBe(typeof(RoleId));
    id.GetValueConverter().ShouldNotBeNull("RoleId must convert to a store type (Guid)");
    role.FindProperty(nameof(Role.Version)).ShouldNotBeNull().IsConcurrencyToken.ShouldBeTrue();
    role.FindProperty(nameof(Role.IsProductRole)).ShouldBeNull("IsProductRole is derived, not stored");
  }

  public static async Task Permissions_own_identity_role_permissions_keyed_by_role_and_permission()
  {
    await using PostgresDbContext db = CreateModelOnlyContext();
    IEntityType owned = OwnedType(db, nameof(Role.Permissions));

    owned.GetSchema().ShouldBe("identity");
    owned.GetTableName().ShouldBe(RoleEntityTypeConfiguration.PermissionsTableName);
    owned.FindPrimaryKey().ShouldNotBeNull().Properties.Select(property => property.GetColumnName())
      .ShouldBe(["RoleId", "PermissionId"]);
    owned.FindProperty(nameof(PermissionGrant.PermissionId)).ShouldNotBeNull().GetMaxLength().ShouldBe(128);
  }

  public static async Task Agent_scopes_own_identity_role_agent_scopes_keyed_by_role_and_scope()
  {
    await using PostgresDbContext db = CreateModelOnlyContext();
    IEntityType owned = OwnedType(db, nameof(Role.AgentScopes));

    owned.GetSchema().ShouldBe("identity");
    owned.GetTableName().ShouldBe(RoleEntityTypeConfiguration.AgentScopesTableName);
    owned.FindPrimaryKey().ShouldNotBeNull().Properties.Select(property => property.GetColumnName())
      .ShouldBe(["RoleId", "Scope"]);
    owned.FindProperty(nameof(AgentScopeGrant.Scope)).ShouldNotBeNull().GetMaxLength().ShouldBe(64);
  }

  public static async Task Navigations_use_the_private_backing_lists()
  {
    await using PostgresDbContext db = CreateModelOnlyContext();
    IEntityType role = db.Model.FindEntityType(typeof(Role)).ShouldNotBeNull();

    role.FindNavigation(nameof(Role.Permissions)).ShouldNotBeNull().FieldInfo.ShouldNotBeNull().Name.ShouldBe("PermissionList");
    role.FindNavigation(nameof(Role.AgentScopes)).ShouldNotBeNull().FieldInfo.ShouldNotBeNull().Name.ShouldBe("AgentScopeList");
  }

  private static IEntityType OwnedType(PostgresDbContext db, string navigation) =>
    db.Model.FindEntityType(typeof(Role)).ShouldNotBeNull()
      .FindNavigation(navigation).ShouldNotBeNull()
      .TargetEntityType;

  private static PostgresDbContext CreateModelOnlyContext()
  {
    DbContextOptions<PostgresDbContext> options = new DbContextOptionsBuilder<PostgresDbContext>()
      .UseNpgsql("Host=127.0.0.1;Database=model-only;Username=unused;Password=unused")
      .Options;
    return new PostgresDbContext(options);
  }
}
