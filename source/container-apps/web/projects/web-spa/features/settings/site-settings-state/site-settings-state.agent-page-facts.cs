#region Purpose
// page_context facts for the page that declares SiteSettings.UpdateSiteSettings.
#endregion

#region Design
// UpdateSiteSettings replaces the whole record and carries a Version token, so the agent must
// echo the current values and version. hasSnapshot is false until the settings have loaded.
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
      ["entraAcceptedTenantIds"] = new JsonArray(
        [.. EntraAcceptedTenantIds.Select(static id => JsonValue.Create(id.ToString("D")))]),
      ["version"] = Version,
    };
  }
}
