#region Purpose
// Registers the Admin/Authentication route and authorize policy; markup and behavior live in AuthenticationPage.razor.
#endregion

#region Design
// Task 225: site Entra policy and passkey prompt belong under Admin, not personal /Settings.
// Policy = settings.write (same as UpdateSiteSettings). settings.read stays self-service for
// /Settings and GetSiteSettings. Not moved into admin.* — see PermissionIds Design (task 225).
// The menu lists it under Admin (NavGroup) for principals with settings.write. SiteSettingsState
// is the Settings slice. The agent may run UpdateSiteSettings here (AgentActions).
// Task 231: policy fields bind ISiteSettingsDetails via EditForm + FluentButton submit
// (RoleForm pattern). Tenant line and drift banner stay outside the form.
// Task 234: FormSection / FormGrid / FormField / FormActions; FluentSelect for
// PasskeyPromptMode; no page-local twe-settings CSS.
#endregion

namespace TimeWarp.Architecture.Features.Admin.SiteSettings;

[Page("/Admin/Authentication", Policy = PermissionIds.SettingsWrite, Navigable = true, Description = "Site authentication settings, including the passkey prompt.", NavGroup = "Admin", NavOrder = 140, AgentActions = [typeof(SiteSettingsState.UpdateSiteSettingsActionSet.Action)])]
[Authorize(Policy = PermissionIds.SettingsWrite)]
[CrossSliceReference(typeof(SiteSettingsState), "Admin Authentication edits the Settings slice site-settings singleton.")]
partial class AuthenticationPage;
