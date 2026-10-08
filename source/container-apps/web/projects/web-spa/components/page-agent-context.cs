#region Purpose
// JSON facts about the current page that an agent uses when filling tool arguments.
#endregion

#region Design
// Lives outside the product slices so the shell and the platform adapter can both read it.
// Settings and Passkeys include the credential rows and the flags those pages' buttons use
// (id, canRevoke, canRename, CanLinkMicrosoft365). Profile and Admin/Authentication include
// the current record, because UpdateProfile and UpdateSiteSettings replace the whole record: the
// agent copies the fields it was not asked to change, and for site settings it must echo the
// Version concurrency token. Values are read from ProfileState / SiteSettingsState as loaded by
// the page; hasSnapshot is false before the first fetch. Enums are member names, as the seam
// writes them. Other routes carry the path only.
// page_context is not a catalog action and does not require approval.
#endregion

namespace TimeWarp.Architecture.Components;

using System.Text.Json.Nodes;
using TimeWarp.Architecture.Features.Applications;
using TimeWarp.Architecture.Features.Identity;

/// <summary>Page facts supplied to the ask UI and to WebMCP.</summary>
public static class PageAgentContext
{
  public const string ToolName = "page_context";

  public const string ToolDescription =
    "Facts about the current page, including item ids and flags the page's buttons use.";

  public const string EmptyInputSchema =
    """{"type":"object","properties":{},"additionalProperties":false}""";

  public static string Describe(IStore store, string? path)
  {
    ArgumentNullException.ThrowIfNull(store);
    string normalized = PageAgentScope.Normalize(path);
    if (normalized.Equals("/Settings", StringComparison.OrdinalIgnoreCase)
      || normalized.Equals("/Passkeys", StringComparison.OrdinalIgnoreCase))
    {
      return DescribeCredentials(store, normalized);
    }

    if (normalized.Equals("/Profile", StringComparison.OrdinalIgnoreCase))
    {
      return DescribeProfile(store, normalized);
    }

    if (normalized.Equals("/Admin/Authentication", StringComparison.OrdinalIgnoreCase))
    {
      return DescribeSiteSettings(store, normalized);
    }

    return new JsonObject { ["path"] = normalized }.ToJsonString();
  }

  private static string DescribeProfile(IStore store, string path)
  {
    ProfileState profile = store.GetState<ProfileState>();
    JsonObject document = new()
    {
      ["path"] = path,
      ["hasSnapshot"] = profile.Alias is not null,
      ["profile"] = new JsonObject
      {
        ["alias"] = profile.Alias,
        ["email"] = profile.Email,
        ["language"] = profile.Language,
        ["region"] = profile.Region,
        ["theme"] = profile.Theme,
        ["notifications"] = profile.Notifications,
      },
    };
    return document.ToJsonString();
  }

  private static string DescribeSiteSettings(IStore store, string path)
  {
    SiteSettingsState settings = store.GetState<SiteSettingsState>();
    JsonObject document = new()
    {
      ["path"] = path,
      ["hasSnapshot"] = settings.HasSnapshot,
      ["siteSettings"] = new JsonObject
      {
        ["entraSignInEnabled"] = settings.EntraSignInEnabled,
        ["entraAllowBootstrap"] = settings.EntraAllowBootstrap,
        ["passkeyPromptMode"] = settings.PasskeyPromptMode.ToString(),
        ["version"] = settings.Version,
      },
    };
    return document.ToJsonString();
  }

  private static string DescribeCredentials(IStore store, string path)
  {
    CredentialsState credentialsState = store.GetState<CredentialsState>();
    JsonArray credentials = [];
    IReadOnlyList<GetCredentials.CredentialSummary> rows = credentialsState.Credentials ?? [];
    foreach (GetCredentials.CredentialSummary credential in rows)
    {
      credentials.Add
      (
        new JsonObject
        {
          ["id"] = credential.Id.Value.ToString("D"),
          ["type"] = credential.Type.ToString(),
          ["nickname"] = credential.Nickname,
          ["label"] = credential.Label,
          ["isActive"] = credential.IsActive,
          ["canRevoke"] = credential.CanRevoke,
          ["canRename"] = credential.CanRename,
        }
      );
    }

    JsonObject document = new()
    {
      ["path"] = path,
      ["canLinkMicrosoft365"] = credentialsState.CanLinkMicrosoft365,
      ["credentials"] = credentials,
    };
    return document.ToJsonString();
  }
}
