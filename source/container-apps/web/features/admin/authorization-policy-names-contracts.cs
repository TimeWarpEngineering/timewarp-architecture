#region Purpose
// Shared capability policy name strings for SPA route guards and server EndpointAuthorize.
#endregion

#region Design
// Task 147-004 (D6): policy names are the string coupling between web-contracts [EndpointAuthorize]
// and web-server AddPolicy registration, and between SPA [Page]/Authorize and RolePolicyGrants.
// Features substrate (bare …Features namespace) so Admin.Roles, Admin.Principals, and SPA can
// reference the same constants without TWA0009 cross-slice coupling — same pattern as RoleIds /
// ModuleIds. Names are capability-shaped (CanView*), not role-shaped; RequireRole(Administrator)
// is composed at policy registration (server) / RolePolicyGrants (SPA), not in the name.
// nameof keeps rename-safe parity with AuthorizationConstants.Policies members of the same name.
#endregion

namespace TimeWarp.Architecture.Features;

/// <summary>Capability policy names shared by SPA and server admin APIs.</summary>
public static class AuthorizationPolicyNames
{
  public const string CanViewRolesPage = nameof(CanViewRolesPage);
  public const string CanViewPrincipalsPage = nameof(CanViewPrincipalsPage);
}
