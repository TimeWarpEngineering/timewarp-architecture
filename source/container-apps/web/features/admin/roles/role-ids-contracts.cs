#region Purpose
// Well-known product role identifiers shared by client and server authorization checks.
#endregion

#region Design
// Product role set (task 147-002): Member, Operator, Administrator, Developer.
// Compile-time Guid constants so SPA and server agree without a round-trip; once issued, an id
// must never change. ERP/accounting sample roles removed — they fought the product narrative.
// GetRoleNameByGuid reflects over public Guid fields so display names cannot drift from the id list.
// Features substrate (bare …Features namespace): Admin.Roles, Identity mocks, and SPA policies
// reference well-known ids without TWA0009 cross-slice coupling.
// Role → permission grants live in RolePermissionSeed (task 182); roles are composition only:
//   Member        — default for every passkey principal; self-service only
//   Operator      — marketplace ops (118); grants reserved until marketplace policies land
//   Administrator — admin.* + self-service
//   Developer     — developer.* + self-service (demos / diagnostics, 147-001)
#endregion

namespace TimeWarp.Architecture.Features;

public static class RoleIds
{
  /// <summary>Default human principal after passkey login — self-service only.</summary>
  public static readonly Guid Member = new("A1B2C3D4-E5F6-4789-A012-3456789ABCDE");

  /// <summary>Marketplace / job oversight human (agentic shop ops). Policies land with 118.</summary>
  public static readonly Guid Operator = new("B2C3D4E5-F6A7-4890-B123-456789ABCDEF");

  /// <summary>Tenant admin — principals, roles, system settings.</summary>
  public static readonly Guid Administrator = new("834B9073-D5FF-40B3-938A-968C23FA76CC");

  /// <summary>Template dogfood — demos, style guide, diagnostics. Not production end-users.</summary>
  public static readonly Guid Developer = new("80EE3E0C-A8B6-45D6-BA27-7DEE2691AA42");

  public static string GetRoleNameByGuid(Guid roleId)
  {
    FieldInfo[] roleFields = typeof(RoleIds).GetFields(BindingFlags.Static | BindingFlags.Public);

    foreach (FieldInfo field in roleFields)
    {
      if (field.FieldType != typeof(Guid)) continue;

      var fieldValue = (Guid)(field.GetValue(null) ?? Guid.Empty);
      if (fieldValue == roleId) return field.Name;
    }

    return "Unknown Role";
  }

  /// <summary>All product role ids (stable order for seed/list UIs).</summary>
  public static IReadOnlyList<Guid> All { get; } =
  [
    Member,
    Operator,
    Administrator,
    Developer,
  ];
}
