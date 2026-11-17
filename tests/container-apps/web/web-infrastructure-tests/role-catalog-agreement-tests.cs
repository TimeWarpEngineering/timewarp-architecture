// Build check for "two things must agree" (task 473): web-domain cannot reference web-contracts or
// timewarp-identity, so RoleCatalog holds copies. This suite sees all three and fails on drift.

namespace Role_Catalog_Agreement_;

using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Admin.Roles.Domain;

public class Copies
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Copies>();

  public static Task Permission_ids_equal_PermissionIds_All()
  {
    RoleCatalog.PermissionIds.ShouldBe(PermissionIds.All, "copy new permission ids from permission-ids-contracts.cs into role-catalog-domain.cs");
    return Task.CompletedTask;
  }

  public static Task Agent_scopes_equal_AgentScopes_All()
  {
    RoleCatalog.AgentScopes.ShouldBe(TimeWarp.Identity.AgentScopes.All, "copy new scopes from agent-scopes.cs into role-catalog-domain.cs");
    return Task.CompletedTask;
  }

  public static Task Product_role_ids_equal_RoleIds_All()
  {
    RoleCatalog.ProductRoleIds.Select(id => id.Value).ShouldBe(RoleIds.All, "copy new product role ids from role-ids-contracts.cs into role-catalog-domain.cs");
    return Task.CompletedTask;
  }
}
