#region Purpose
// Registers the Admin/AI route. Markup and behavior live in AiPage.razor. The page sets the site
// xAI key, whose key Ask uses, and renames the assistant.
#endregion

#region Design
// settings.write, same gate as the key endpoints and Admin → Authentication. The menu lists it
// under Admin. The agent may run UpdateAssistantName and UpdateXaiByokMode here. Both live on
// site settings, so the page takes a cross-slice reference on SiteSettingsState. The set and
// clear key actions stay off the agent: those move a secret.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Ai;

/// <summary>Admin page for the site xAI API key, whose key Ask uses, and the assistant name.</summary>
[Page("/Admin/AI", Policy = PermissionIds.SettingsWrite, Navigable = true, Description = "Set the site xAI API key, whose key Ask uses, and the assistant name.", NavGroup = "Admin", NavOrder = 150, AgentActions = [typeof(SiteSettingsState.UpdateAssistantNameActionSet.Action), typeof(SiteSettingsState.UpdateXaiByokModeActionSet.Action)])]
[Authorize(Policy = PermissionIds.SettingsWrite)]
[CrossSliceReference(typeof(SiteSettingsState), "Admin AI renames the assistant and sets whose xAI key Ask uses, both stored on the Settings slice site-settings singleton.")]
partial class AiPage;
