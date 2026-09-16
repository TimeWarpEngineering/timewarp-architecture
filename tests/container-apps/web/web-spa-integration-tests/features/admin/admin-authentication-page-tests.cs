#region Purpose
// Route and policy proofs for Admin/Authentication (task 225).
#endregion

#region Design
// Pure generated page accessors — no SPA host. Policy is settings.write (same as UpdateSiteSettings).
// Non-admin cannot see the nav entry because TimeWarpNavLink authorizes TPage.Policy and the Admin
// category is gated by admin.access.
#endregion

namespace AdminAuthenticationPage_;

using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Admin.SiteSettings;
using TimeWarp.Architecture.Features.Applications;
using TimeWarp.Architecture.Features.Settings;

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
}
