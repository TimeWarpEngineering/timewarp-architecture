#region Purpose
// Policy names shared between authorization policy registration and [Authorize]/AuthorizeView consumers.
#endregion

#region Design
// Policy names are the string coupling between policy registration and attribute/markup
// usage; nameof-based constants keep them typo-proof and refactor-safe.
// Policies are per-surface (page, nav section, capability) rather than per-role so each UI
// element can be gated independently of how roles are composed.
#endregion

namespace TimeWarp.Architecture;

public static class AuthorizationConstants
{
  public static class Policies
  {
    // General
    public const string Anonymous = nameof(Anonymous);

    /// <summary>Any signed-in principal (identity-session, mock, or Entra). Not role-gated.</summary>
    public const string Authenticated = nameof(Authenticated);

    // Self-service (Member+ product roles via RolePolicyGrants)
    public const string CanViewOwnProfile = nameof(CanViewOwnProfile);
    public const string CanViewSettings = nameof(CanViewSettings);

    // Pages
    public const string CanViewAdminPage = nameof(CanViewAdminPage);
    public const string CanViewDeveloperPage = nameof(CanViewDeveloperPage);
    public const string CanViewUserClaimsPage = nameof(CanViewUserClaimsPage);
    public const string CanViewRolesPage = nameof(CanViewRolesPage);
    public const string CanViewPrincipalsPage = nameof(CanViewPrincipalsPage);

    // Navigation
    public const string CanViewDeveloperSidebarNavSection = nameof(CanViewDeveloperSidebarNavSection);
    public const string CanViewAdminSidebarNavSection = nameof(CanViewAdminSidebarNavSection);

    // Developer
    public const string CanViewUserClaims = nameof(CanViewUserClaims);
  }
}
