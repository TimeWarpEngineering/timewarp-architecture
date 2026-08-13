#region Purpose
// ASP.NET authorization requirement that names one permission id to evaluate.
#endregion

#region Design
// Task 182-002: policy name == PermissionIds string == PermissionRequirement.PermissionId (1:1).
// Handler (server) is the only place that decides success — always via IPermissionEvaluator,
// never by inspecting role/permission claims. Lives in contracts so PermissionPolicyRegistration
// can construct requirements without a server reference. SPA (182-003) does NOT use this type —
// WASM registers RequireClaim policies via AddPermissionClaimPolicies instead.
#endregion

namespace TimeWarp.Architecture.Features;

using Microsoft.AspNetCore.Authorization;

/// <summary>Requires the principal to hold a single permission (capability) id.</summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
  public PermissionRequirement(string permissionId)
  {
    PermissionId = string.IsNullOrWhiteSpace(permissionId)
      ? throw new ArgumentException("Permission id is required.", nameof(permissionId))
      : permissionId;
  }

  /// <summary>Permission id / policy name (see <see cref="PermissionIds"/>).</summary>
  public string PermissionId { get; }
}
