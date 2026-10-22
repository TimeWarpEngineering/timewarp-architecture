#region Purpose
// Registers the Admin/AI route. Markup and behavior live in AiPage.razor.
#endregion

#region Design
// settings.write, same gate as the key endpoints and Admin → Authentication. The menu lists it
// under Admin. The agent is not given the set or clear actions: those move a secret.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Ai;

/// <summary>Admin page for the site xAI API key.</summary>
[Page("/Admin/AI", Policy = PermissionIds.SettingsWrite, Navigable = true, Description = "Set the site xAI API key used by Ask.", NavGroup = "Admin", NavOrder = 150)]
[Authorize(Policy = PermissionIds.SettingsWrite)]
partial class AiPage;
