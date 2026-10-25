#region Purpose
// Route and policy proofs for Admin/Authentication (task 225).
#endregion

#region Design
// Pure generated page accessors — no SPA host. Policy is settings.write (same as UpdateSiteSettings).
// Only a principal with settings.write sees the nav entry: NavMenu renders NavigationDestinations,
// which keeps a page only when its own Policy authorizes the principal.
#endregion

namespace AdminAuthenticationPage_;

using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Admin.SiteSettings;
using TimeWarp.Architecture.Features.Applications;
using TimeWarp.Architecture.Features.Settings;
using TimeWarp.Identity;

[TestTag("Unit")]
public class AuthenticationPage_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AuthenticationPage_Should_>();

  public static Task Use_Admin_Route_And_SettingsWrite_Policy()
  {
    AuthenticationPage.GetPageUrl().ShouldBe("/Admin/Authentication");
    AuthenticationPage.Policy.ShouldBe(PermissionIds.SettingsWrite);
    AuthenticationPage.Title.ShouldBe("Authentication");
    SiteSettingsConfigurationDrift.AdminPageRoute.ShouldBe(AuthenticationPage.GetPageUrl());
    return Task.CompletedTask;
  }

  public static Task Settings_Page_Should_Remain_Self_Service()
  {
    SettingsPage.GetPageUrl().ShouldBe("/Settings");
    SettingsPage.Policy.ShouldBe(PermissionIds.SettingsRead);
    SettingsPage.Policy.ShouldNotBe(AuthenticationPage.Policy);
    return Task.CompletedTask;
  }

  public static Task Format_Should_Show_The_Domain_Or_The_Bare_Guid()
  {
    Guid tenantId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    AuthenticationPage.FormatAcceptedTenant(new EntraAcceptedTenant(tenantId, "contoso.com"))
      .ShouldBe($"contoso.com ({tenantId:D})");
    AuthenticationPage.FormatAcceptedTenant(EntraAcceptedTenant.IdOnly(tenantId))
      .ShouldBe(tenantId.ToString("D"));
    return Task.CompletedTask;
  }
}
