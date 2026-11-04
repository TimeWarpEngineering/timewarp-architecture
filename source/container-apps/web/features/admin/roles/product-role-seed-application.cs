#region Purpose
// The four product roles' seeded names and descriptions, for the in-memory role store.
#endregion

#region Design
// Postgres gets the same rows from migration AddRoles (InsertData with Guid literals, the house
// precedent from AddRolePermissions); role-migration-seed-tests.cs in web-infrastructure-tests
// fails when the migration's ids, names, or descriptions differ from this list or its ids differ
// from RoleIds.All. Names are the RoleIds field names, the same values the retired dictionary
// RoleStore held. Both are editable through UpdateRole afterwards; the seed is the starting state.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

using TimeWarp.Architecture.Features;

/// <summary>Seed rows for the four product roles, in RoleIds.All order.</summary>
public static class ProductRoleSeed
{
  public static IReadOnlyList<Entry> Roles { get; } =
  [
    new(RoleIds.Member, nameof(RoleIds.Member), "Default human principal after passkey login; self-service only."),
    new(RoleIds.Operator, nameof(RoleIds.Operator), "Marketplace and job oversight (agentic shop ops)."),
    new(RoleIds.Administrator, nameof(RoleIds.Administrator), "Tenant admin: principals, roles, system settings."),
    new(RoleIds.Developer, nameof(RoleIds.Developer), "Template dogfood: demos and diagnostics."),
  ];

  /// <summary>Seeded name and description for one product role.</summary>
  public sealed record Entry(Guid Id, string Name, string Description);
}
