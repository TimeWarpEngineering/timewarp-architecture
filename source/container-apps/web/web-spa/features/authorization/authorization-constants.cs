namespace TimeWarp.Architecture;

public static class AuthorizationConstants
{
  public static class Policies
  {
    // General
    public const string Anonymous = nameof(Anonymous);

    // Pages
    public const string CanViewAdminPage = nameof(CanViewAdminPage);
    public const string CanViewDeveloperPage = nameof(CanViewDeveloperPage);
    public const string CanViewUserClaimsPage = nameof(CanViewUserClaimsPage);
    public const string CanViewRolesPage = nameof(CanViewRolesPage);

    // Navigation
    public const string CanViewDeveloperSidebarNavSection = nameof(CanViewDeveloperSidebarNavSection);
    public const string CanViewAdminSidebarNavSection = nameof(CanViewAdminSidebarNavSection);

    // Developer
    public const string CanViewUserClaims = nameof(CanViewUserClaims);
  }
}
