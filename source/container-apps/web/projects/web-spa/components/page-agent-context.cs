#region Purpose
// JSON facts about the current page that an agent reads before it calls a tool.
#endregion

#region Design
// Every route gets the same envelope: path, title, purpose, headings, summary, forms, buttons,
// items, tools, and screenshot. Title and purpose come from PageRegistry (the [Page] Description),
// not a second list. An unknown route uses the path as the title and a fixed fallback purpose.
// A registry URL other than / also matches as a prefix, longest first, so /Feedback/{id} takes
// the Feedback title. The open item then replaces purpose with the one-item sentence.
// Headings, summary, forms, buttons, and items are the bounded text walk PageSurfaceJsModule
// stores on AgentSurfaceState. Caps: 40 headings, 2,000 summary characters, 30 buttons, 10 forms,
// 20 fields, 40 items, 500 elements in the walk, and 12,000 characters for this document. Text is
// always present; an empty walk yields empty arrays and an empty summary. screenshot is null:
// pixel capture is a later opt-in, not this document.
// Settings, Passkeys, Profile, Admin/Authentication, and Feedback keep their fact fields (ids,
// flags, the records a replace-whole command must echo, filings capped at 20). The draft body is
// not included. tools is the names the caller is offering, or, when omitted, navigate plus the
// page-bound names (including human-only) plus page_context. Live callers pass the offered names.
// page_context is not a catalog action and does not require approval.
#endregion

namespace TimeWarp.Architecture.Components;

using System.Text.Json.Nodes;
using TimeWarp.Architecture.Features.Applications;
using TimeWarp.Architecture.Features.Feedback;
using TimeWarp.Architecture.Features.Identity;

/// <summary>Page facts supplied to the ask UI and to WebMCP.</summary>
public static class PageAgentContext
{
  public const string ToolName = "page_context";

  public const string ToolDescription =
    "Facts about the current page: title, purpose, visible text, item ids, flags, and tool names.";

  public const string EmptyInputSchema =
    """{"type":"object","properties":{},"additionalProperties":false}""";

  public const string SurfaceRootSelector = ".twe-page__body";

  public const string UnknownPurpose = "This route has no navigation-destination description.";

  public const int SummaryCap = 2_000;

  public const int DocumentCap = 12_000;

  private const int FilingCap = 20;

  private const int HeadingCap = 40;

  private const int ButtonCap = 30;

  private const int FormCap = 10;

  private const int ItemCap = 40;

  public static string Describe(IStore store, string? path) => Describe(store, path, toolNames: null);

  public static string Describe(IStore store, string? path, IReadOnlyList<string>? toolNames)
  {
    ArgumentNullException.ThrowIfNull(store);
    string normalized = PageAgentScope.Normalize(path);
    JsonObject document = Envelope(store, normalized, toolNames);
    if (normalized.Equals("/Settings", StringComparison.OrdinalIgnoreCase)
      || normalized.Equals("/Passkeys", StringComparison.OrdinalIgnoreCase))
    {
      AddCredentials(document, store);
    }
    else if (normalized.Equals("/Profile", StringComparison.OrdinalIgnoreCase))
    {
      AddProfile(document, store);
    }
    else if (normalized.Equals("/Admin/Authentication", StringComparison.OrdinalIgnoreCase))
    {
      AddSiteSettings(document, store);
    }
    else if (normalized.Equals("/Feedback", StringComparison.OrdinalIgnoreCase))
    {
      AddFeedback(document, store, openFeedbackId: null);
    }
    else if (TryOpenFeedbackId(normalized, out Guid openFeedbackId))
    {
      document["purpose"] = "One feedback item you filed.";
      AddFeedback(document, store, openFeedbackId);
    }

    return Bound(document);
  }

  private static JsonObject Envelope(IStore store, string path, IReadOnlyList<string>? toolNames)
  {
    (string title, string purpose) = Identity(path);
    JsonObject document = new()
    {
      ["path"] = path,
      ["title"] = title,
      ["purpose"] = purpose,
      ["headings"] = new JsonArray(),
      ["summary"] = "",
      ["forms"] = new JsonArray(),
      ["buttons"] = new JsonArray(),
      ["items"] = new JsonArray(),
      ["tools"] = ToolNames(path, toolNames),
      ["screenshot"] = JsonNode.Parse("null"),
    };
    MergeSurface(document, store.GetState<AgentSurfaceState>().PageSurfaceJson);
    return document;
  }

  private static (string Title, string Purpose) Identity(string path)
  {
    foreach (PageRegistryEntry entry in PageRegistry.All)
    {
      if (string.Equals(entry.Url, path, StringComparison.OrdinalIgnoreCase))
      {
        return (entry.Title, PurposeOf(entry));
      }
    }

    PageRegistryEntry? prefix = null;
    foreach (PageRegistryEntry entry in PageRegistry.All)
    {
      if (entry.Url.Length <= 1)
      {
        continue;
      }

      if (!path.StartsWith(entry.Url + "/", StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      if (prefix is null || entry.Url.Length > prefix.Url.Length)
      {
        prefix = entry;
      }
    }

    return prefix is null ? (path, UnknownPurpose) : (prefix.Title, PurposeOf(prefix));
  }

  private static string PurposeOf(PageRegistryEntry entry) =>
    string.IsNullOrWhiteSpace(entry.Description) ? UnknownPurpose : entry.Description;

  private static JsonArray ToolNames(string path, IReadOnlyList<string>? toolNames)
  {
    JsonArray names = [];
    if (toolNames is not null)
    {
      foreach (string name in toolNames)
      {
        names.Add(name);
      }

      return names;
    }

    names.Add(AgentNavigate.ToolName);
    foreach (string name in PageAgentScope.ActionNamesFor(path))
    {
      names.Add(name);
    }

    names.Add(ToolName);
    return names;
  }

  private static void MergeSurface(JsonObject document, string? surfaceJson)
  {
    if (string.IsNullOrWhiteSpace(surfaceJson))
    {
      return;
    }

    JsonNode? parsed;
    try
    {
      parsed = JsonNode.Parse(surfaceJson);
    }
    catch (JsonException)
    {
      return;
    }

    if (parsed is not JsonObject surface)
    {
      return;
    }

    CopyArray(document, surface, "headings", HeadingCap);
    CopyArray(document, surface, "forms", FormCap);
    CopyArray(document, surface, "buttons", ButtonCap);
    CopyArray(document, surface, "items", ItemCap);
    if (surface["summary"] is JsonValue summaryValue && summaryValue.TryGetValue(out string? summary) && summary is not null)
    {
      document["summary"] = summary.Length <= SummaryCap ? summary : summary[..SummaryCap];
    }
  }

  private static void CopyArray(JsonObject document, JsonObject surface, string name, int cap)
  {
    if (surface[name] is not JsonArray source)
    {
      return;
    }

    JsonArray copy = [];
    int count = 0;
    foreach (JsonNode? item in source)
    {
      if (count == cap)
      {
        break;
      }

      count++;
      if (item is not null)
      {
        copy.Add(item.DeepClone());
      }
    }

    document[name] = copy;
  }

  private static string Bound(JsonObject document)
  {
    string json = document.ToJsonString();
    if (json.Length <= DocumentCap)
    {
      return json;
    }

    document["summary"] = "";
    json = document.ToJsonString();
    if (json.Length <= DocumentCap)
    {
      return json;
    }

    document["headings"] = new JsonArray();
    return document.ToJsonString();
  }

  private static bool TryOpenFeedbackId(string normalized, out Guid feedbackItemId)
  {
    const string prefix = "/Feedback/";
    if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
      && Guid.TryParse(normalized[prefix.Length..], out feedbackItemId))
    {
      return true;
    }

    feedbackItemId = default;
    return false;
  }

  private static void AddFeedback(JsonObject document, IStore store, Guid? openFeedbackId)
  {
    FeedbackState feedback = store.GetState<FeedbackState>();
    document["page"] = "Feedback";
    AddFilings(document, feedback);
    document["emailCopyAvailable"] = feedback.EmailCopyAvailable;
    document["draftAttachmentCount"] = feedback.DraftAttachments.Count;
    if (feedback.ComposerActive)
    {
      document["draft"] = new JsonObject
      {
        ["kind"] = feedback.DraftKind,
        ["hasTitle"] = feedback.DraftHasTitle,
        ["hasBody"] = feedback.DraftHasBody,
      };
    }

    if (openFeedbackId is Guid openId)
    {
      document["openFeedbackId"] = openId.ToString("D");
      GetFeedback.Response? current = feedback.Current;
      if (current is not null && current.FeedbackItemId == openId)
      {
        document["openItem"] = new JsonObject
        {
          ["id"] = current.FeedbackItemId.ToString("D"),
          ["title"] = current.Title,
          ["kind"] = current.Kind.ToString(),
        };
      }
    }
  }

  private static void AddFilings(JsonObject document, FeedbackState feedback)
  {
    JsonArray filings = [];
    int shown = 0;
    foreach (ListMyFeedback.Item item in feedback.Items)
    {
      if (shown == FilingCap)
      {
        break;
      }

      shown++;
      filings.Add
      (
        new JsonObject
        {
          ["id"] = item.FeedbackItemId.ToString("D"),
          ["title"] = item.Title,
          ["kind"] = item.Kind.ToString(),
        }
      );
    }

    document["filingsLoaded"] = feedback.FilingsLoaded;
    document["filingCount"] = feedback.Items.Count;
    document["filings"] = filings;
  }

  private static void AddProfile(JsonObject document, IStore store)
  {
    ProfileState profile = store.GetState<ProfileState>();
    document["hasSnapshot"] = profile.Alias is not null;
    document["profile"] = new JsonObject
    {
      ["alias"] = profile.Alias,
      ["email"] = profile.Email,
      ["language"] = profile.Language,
      ["region"] = profile.Region,
      ["theme"] = profile.Theme,
      ["notifications"] = profile.Notifications,
    };
  }

  private static void AddSiteSettings(JsonObject document, IStore store)
  {
    SiteSettingsState settings = store.GetState<SiteSettingsState>();
    document["hasSnapshot"] = settings.HasSnapshot;
    document["siteSettings"] = new JsonObject
    {
      ["entraSignInEnabled"] = settings.EntraSignInEnabled,
      ["entraAllowBootstrap"] = settings.EntraAllowBootstrap,
      ["passkeyPromptMode"] = settings.PasskeyPromptMode.ToString(),
      ["version"] = settings.Version,
    };
  }

  private static void AddCredentials(JsonObject document, IStore store)
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

    document["canLinkMicrosoft365"] = credentialsState.CanLinkMicrosoft365;
    document["credentials"] = credentials;
  }
}
