#region Purpose
// Registers the Admin/AI route. Markup and behavior live in AiPage.razor. The page sets the site
// xAI key and renames the assistant.
#endregion

#region Design
// settings.write, same gate as the key endpoints and Admin → Authentication. The menu lists it
// under Admin. The agent is not given the set or clear actions: those move a secret.
// The agent may run UpdateAssistantName here. The name is site settings, so the page takes a
// cross-slice reference on SiteSettingsState.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Ai;

/// <summary>Admin page for the site xAI API key and the assistant name.</summary>
[Page("/Admin/AI", Policy = PermissionIds.SettingsWrite, Navigable = true, Description = "Set the site xAI API key and the assistant name used by Ask.", NavGroup = "Admin", NavOrder = 150, AgentActions = [typeof(SiteSettingsState.UpdateAssistantNameActionSet.Action)])]
[Authorize(Policy = PermissionIds.SettingsWrite)]
[CrossSliceReference(typeof(SiteSettingsState), "Admin AI renames the assistant stored on the Settings slice site-settings singleton.")]
partial class AiPage;
