// Role aggregate (task 473): fail-closed Create, UpdateDetails, full-replace children against the
// domain catalogs, the product-role delete guard, and the nested Invariants (save-time half).

namespace Role_;

using System.Reflection;
using FluentValidation;
using FluentValidation.Results;

public class Create
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Create>();

  public static Task Sets_details_and_starts_with_no_children()
  {
    var role = Role.Create("Auditor", "Read-only access.");

    role.Name.ShouldBe("Auditor");
    role.Description.ShouldBe("Read-only access.");
    role.Id.IsEmpty.ShouldBeFalse();
    role.Permissions.ShouldBeEmpty();
    role.AgentScopes.ShouldBeEmpty();
    role.IsProductRole.ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Rejects_blank_name_or_description()
  {
    Should.Throw<ArgumentException>(() => Role.Create(" ", "Read-only access."));
    Should.Throw<ArgumentException>(() => Role.Create("Auditor", ""));
    Should.Throw<ArgumentException>(() => Role.Create(null!, "Read-only access."));
    return Task.CompletedTask;
  }

  public static Task Fixed_id_overload_keeps_the_id_and_rejects_empty()
  {
    var id = RoleId.New();
    Role.Create(id, "Auditor", "Read-only access.").Id.ShouldBe(id);
    Should.Throw<ArgumentException>(() => Role.Create(default(RoleId), "Auditor", "Read-only access."));
    return Task.CompletedTask;
  }
}

public class Update_details
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Update_details>();

  public static Task Replaces_name_and_description()
  {
    var role = Role.Create("Auditor", "Read-only access.");
    role.UpdateDetails("Reviewer", "Reviews things.");
    role.Name.ShouldBe("Reviewer");
    role.Description.ShouldBe("Reviews things.");
    return Task.CompletedTask;
  }

  public static Task Rejects_blank_values_and_keeps_state()
  {
    var role = Role.Create("Auditor", "Read-only access.");
    Should.Throw<ArgumentException>(() => role.UpdateDetails("", "Reviews things."));
    Should.Throw<ArgumentException>(() => role.UpdateDetails("Reviewer", "  "));
    role.Name.ShouldBe("Auditor");
    role.Description.ShouldBe("Read-only access.");
    return Task.CompletedTask;
  }
}

public class Set_permissions
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Set_permissions>();

  public static Task Full_replace_keeps_requested_order_and_collapses_duplicates()
  {
    var role = Role.Create("Auditor", "Read-only access.");
    role.SetPermissions(["profile.read", "admin.access", "profile.read"]);
    role.Permissions.Select(permission => permission.PermissionId).ShouldBe(["profile.read", "admin.access"]);

    role.SetPermissions(["settings.read"]);
    role.Permissions.Select(permission => permission.PermissionId).ShouldBe(["settings.read"]);

    role.SetPermissions([]);
    role.Permissions.ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task Keeps_the_instance_of_a_permission_already_present()
  {
    var role = Role.Create("Auditor", "Read-only access.");
    role.SetPermissions(["profile.read", "settings.read"]);
    PermissionGrant kept = role.Permissions[0];

    role.SetPermissions(["admin.access", "profile.read"]);

    role.Permissions.Count.ShouldBe(2);
    ReferenceEquals(role.Permissions[1], kept).ShouldBeTrue("an unchanged child must stay the same instance (EF keeps its row)");
    return Task.CompletedTask;
  }

  public static Task Rejects_an_id_outside_the_catalog_and_keeps_state()
  {
    var role = Role.Create("Auditor", "Read-only access.");
    role.SetPermissions(["profile.read"]);
    Should.Throw<ArgumentException>(() => role.SetPermissions(["profile.read", "not.a.permission"]));
    Should.Throw<ArgumentException>(() => role.SetPermissions([" "]));
    role.Permissions.Select(permission => permission.PermissionId).ShouldBe(["profile.read"]);
    return Task.CompletedTask;
  }

  public static Task Child_rejects_blank_and_over_length_values()
  {
    Should.Throw<ArgumentException>(() => new PermissionGrant(""));
    Should.Throw<ArgumentOutOfRangeException>(() => new PermissionGrant(new string('a', PermissionGrant.MaxLength + 1)));
    new PermissionGrant("profile.read").ShouldBe(new PermissionGrant("profile.read"));
    return Task.CompletedTask;
  }
}

public class Set_agent_scopes
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Set_agent_scopes>();

  public static Task Full_replace_and_empty_clears()
  {
    var role = Role.Create("Auditor", "Read-only access.");
    role.SetAgentScopes(["feedback:read", "identity:read", "feedback:read"]);
    role.AgentScopes.Select(scope => scope.Scope).ShouldBe(["feedback:read", "identity:read"]);

    role.SetAgentScopes(["demo:invoke"]);
    role.AgentScopes.Select(scope => scope.Scope).ShouldBe(["demo:invoke"]);

    role.SetAgentScopes([]);
    role.AgentScopes.ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task Rejects_a_scope_outside_the_catalog()
  {
    var role = Role.Create("Auditor", "Read-only access.");
    Should.Throw<ArgumentException>(() => role.SetAgentScopes(["identity.read"]));
    role.AgentScopes.ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task Child_rejects_over_length_values()
  {
    Should.Throw<ArgumentOutOfRangeException>(() => new AgentScopeGrant(new string('a', AgentScopeGrant.MaxLength + 1)));
    return Task.CompletedTask;
  }
}

public class Product_role
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Product_role>();

  public static Task Product_roles_are_not_removable()
  {
    foreach (RoleId id in RoleCatalog.ProductRoleIds)
    {
      var role = Role.Create(id, "Product", "A product role.");
      role.IsProductRole.ShouldBeTrue();
      Should.Throw<InvalidOperationException>(role.EnsureRemovable);
    }

    return Task.CompletedTask;
  }

  public static Task Custom_roles_are_removable()
  {
    var role = Role.Create("Auditor", "Read-only access.");
    Should.NotThrow(role.EnsureRemovable);
    return Task.CompletedTask;
  }
}

public class Invariants
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Invariants>();

  public static Task Valid_role_passes()
  {
    var role = Role.Create("Auditor", "Read-only access.");
    role.SetPermissions(["profile.read"]);
    role.SetAgentScopes(["identity:read"]);
    Validate(role).IsValid.ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Permission_outside_the_catalog_fails()
  {
    // A row loaded from storage can hold an id the catalog no longer has; Invariants is the belt.
    var role = Role.Create("Auditor", "Read-only access.");
    BackingList<PermissionGrant>(role, "PermissionList").Add(new PermissionGrant("retired.permission"));

    ValidationResult result = Validate(role);
    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(error => error.PropertyName.StartsWith(nameof(Role.Permissions), StringComparison.Ordinal));
    return Task.CompletedTask;
  }

  public static Task Agent_scope_outside_the_catalog_fails()
  {
    var role = Role.Create("Auditor", "Read-only access.");
    BackingList<AgentScopeGrant>(role, "AgentScopeList").Add(new AgentScopeGrant("retired:scope"));

    ValidationResult result = Validate(role);
    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(error => error.PropertyName.StartsWith(nameof(Role.AgentScopes), StringComparison.Ordinal));
    return Task.CompletedTask;
  }

  private static ValidationResult Validate(Role role)
  {
    Type invariantsType = typeof(Role).GetNestedType("Invariants", BindingFlags.NonPublic)
      .ShouldNotBeNull("Role must nest a private Invariants validator (TWA0011/TWA0012)");
    var validator = (IValidator<Role>)Activator.CreateInstance(invariantsType, nonPublic: true)!;
    return validator.Validate(role);
  }

  private static List<T> BackingList<T>(Role role, string fieldName) =>
    (List<T>)typeof(Role).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
      .ShouldNotBeNull($"Role.{fieldName} is the EF field-access backing list")
      .GetValue(role)!;
}
