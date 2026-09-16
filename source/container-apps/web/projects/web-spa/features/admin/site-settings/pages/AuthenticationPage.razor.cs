#region Purpose
// Registers the Admin/Authentication route and authorize policy; markup and behavior live in AuthenticationPage.razor.
#endregion

#region Design
// Task 225: site Entra policy and passkey prompt belong under Admin, not personal /Settings.
// Policy = settings.write (same as UpdateSiteSettings). settings.read stays self-service for
// /Settings and GetSiteSettings. Not moved into admin.* — see PermissionIds Design (task 225).
// Nav sits in the Admin category (admin.access). SiteSettingsState is the Settings slice.
#endregion

namespace TimeWarp.Architecture.Features.Admin.SiteSettings;

[Page("/Admin/Authentication", Policy = PermissionIds.SettingsWrite)]
[Authorize(Policy = PermissionIds.SettingsWrite)]
[CrossSliceReference(typeof(SiteSettingsState), "Admin Authentication edits the Settings slice site-settings singleton.")]
partial class AuthenticationPage;
