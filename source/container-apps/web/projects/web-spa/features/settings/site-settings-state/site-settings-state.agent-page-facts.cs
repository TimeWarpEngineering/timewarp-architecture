#region Purpose
// page_context facts for pages that declare a SiteSettingsState action.
#endregion

#region Design
// UpdateSiteSettings replaces the authentication policy and carries a Version token, so the agent
// must echo those fields and the version. It does not send assistantName. UpdateAssistantName
// sends assistantName and the same version. hasSnapshot is false until the settings have loaded.
// Both pages that declare an action on this state see the same snapshot.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using System.Text.Json.Nodes;
using TimeWarp.Architecture.Features.Applications;

partial class SiteSettingsState : IAgentPageFacts
{
  /// <inheritdoc />
  public void AddAgentPageFacts(JsonObject document, string path)
  {
    ArgumentNullException.ThrowIfNull(document);
    document["hasSnapshot"] = HasSnapshot;
    document["siteSettings"] = new JsonObject
    {
      ["entraSignInEnabled"] = EntraSignInEnabled,
      ["entraAllowBootstrap"] = EntraAllowBootstrap,
      ["passkeyPromptMode"] = PasskeyPromptMode.ToString(),
      ["assistantName"] = AssistantName,
      ["entraTenantAcceptanceMode"] = EntraTenantAcceptanceMode.ToString(),
      ["entraAcceptedTenants"] = new JsonArray(
        [.. EntraAcceptedTenants.Select(static tenant => new JsonObject
        {
          ["tenantId"] = tenant.TenantId.ToString("D"),
          ["domain"] = tenant.Domain,
        })]),
      ["version"] = Version,
    };
  }
}
