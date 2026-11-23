#region Purpose
// page_context facts for pages that declare a SiteSettingsState action.
#endregion

#region Design
// UpdateSiteSettings replaces the authentication policy and carries a Version token, so the agent
// must echo those fields and the version. It does not send assistantName. UpdateAssistantName
// sends assistantName and the same version. UpdateXaiByokMode sends xaiByokMode and the same
// version. The host key lives on XaiApiKeyState, so this snapshot does not carry it. hasSnapshot
// is false until fetch has stored a GetSiteSettings.Response. A missing response writes null
// facts rather than the type's defaults. Both pages that declare an action on this state see
// the same snapshot.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using System.Text.Json.Nodes;
using TimeWarp.Architecture.Features.Applications;
using TimeWarp.Identity;

partial class SiteSettingsState : IAgentPageFacts
{
  /// <inheritdoc />
  public void AddAgentPageFacts(JsonObject document, string path)
  {
    ArgumentNullException.ThrowIfNull(document);
    GetSiteSettings.Response? settings = SiteSettings;
    document["hasSnapshot"] = settings is not null;
    JsonArray tenants = [];
    if (settings is not null)
    {
      foreach (EntraAcceptedTenant tenant in settings.EntraAcceptedTenants)
      {
        tenants.Add
        (
          new JsonObject
          {
            ["tenantId"] = tenant.TenantId.ToString("D"),
            ["domain"] = tenant.Domain,
          }
        );
      }
    }

    JsonObject siteSettings = new()
    {
      ["entraSignInEnabled"] = null,
      ["entraAllowBootstrap"] = null,
      ["passkeyPromptMode"] = settings?.PasskeyPromptMode.ToString(),
      ["assistantName"] = settings?.AssistantName,
      ["xaiByokMode"] = settings?.XaiByokMode.ToString(),
      ["entraTenantAcceptanceMode"] = settings?.EntraTenantAcceptanceMode.ToString(),
      ["entraAcceptedTenants"] = tenants,
      ["version"] = null,
    };
    if (settings is not null)
    {
      siteSettings["entraSignInEnabled"] = JsonValue.Create(settings.EntraSignInEnabled);
      siteSettings["entraAllowBootstrap"] = JsonValue.Create(settings.EntraAllowBootstrap);
      siteSettings["version"] = JsonValue.Create(settings.Version);
    }

    document["siteSettings"] = siteSettings;
  }
}
