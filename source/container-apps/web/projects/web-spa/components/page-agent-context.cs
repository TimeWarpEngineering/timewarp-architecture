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
// returns. Live callers (the WebMCP dispatcher and Ask's page_context function) walk the page when
// the tool is called and pass that JSON in. When there is no live walk (TrySummarizeAsync
// returned null), the copy SyncWebMcp stored on AgentSurfaceState is used only when its
// PageSurfacePath is this path, so one page's text never appears on another. Caps, applied
// again here: 40 headings of 200 characters, 2,000 summary characters, 30 buttons of 120, 10
// forms (name 120, 20 fields of 80), 40 items (id 80, text 200), and 500 elements in the walk.
// The document cap is 12,000 characters by default; Ask passes a smaller one for its
// instructions. Over the cap, surface text is dropped whole, in order summary, items, forms,
// buttons, headings, so the JSON stays valid. Page facts are bounded by their own caps and are
// not dropped. Text is always present; an empty walk yields empty arrays and an empty summary.
// screenshot is null: pixel capture is a later opt-in, not this document.
// Page facts have no per-route cases (task 300): the states that own the actions the page
// declares ([Page(AgentActions = ...)], via PageAgentScope.StateTypesFor) add their own when they
// implement IAgentPageFacts (credentials, profile, site settings, feedback): ids, flags, the
// records a replace-whole command must echo, filings capped at 20. A page with no agent actions
// gets the envelope only. tools is the names the caller is offering, or, when omitted, navigate,
// site_map and page_context. Live callers pass the offered names.
// page_context is not a catalog action and does not require approval.
// references (task 345) lists the @ references the person chose in this conversation
// (AskReferenceState, current ConversationGeneration only): token, type prefix, idType, id, and
// label (capped at 120 characters). It is what resolves an @prefix:id token in a message to a
// typed id the agent can pass to a tool parameter of that type. It names, it does not grant:
// every tool call is re-authorized by the server. It is a page fact and is not dropped by the cap.
// tags (task 346) lists the elements the person tagged with Ask's Tag and already sent in this
// conversation (AskTagState.SentFor, current ConversationGeneration only), in the AskTagPayload
// shape: kind, label, text, dataQa, landmark, route, and the 345 reference when the element is a
// referenceable resource. Chips not yet sent are not listed; they travel with their message.
// tags is a page fact too and is not dropped by the cap.
#endregion

namespace TimeWarp.Architecture.Components;

using System.Text.Json.Nodes;
using TimeWarp.Architecture.Features.Applications;

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

  private const int HeadingCap = 40;

  private const int ButtonCap = 30;

  private const int FormCap = 10;

  private const int ItemCap = 40;

  private const int HeadingTextCap = 200;

  private const int ButtonTextCap = 120;

  private const int FormNameCap = 120;

  private const int FieldCap = 20;

  private const int FieldTextCap = 80;

  private const int ItemIdCap = 80;

  private const int ItemTextCap = 200;

  private const int ReferenceLabelCap = 120;

  public static string Describe(IStore store, string? path) => Describe(store, path, toolNames: null);

  public static string Describe(IStore store, string? path, IReadOnlyList<string>? toolNames) =>
    Describe(store, path, toolNames, liveSurfaceJson: null, DocumentCap);

  public static string Describe
  (
    IStore store,
    string? path,
    IReadOnlyList<string>? toolNames,
    string? liveSurfaceJson,
    int maxLength
  )
  {
    ArgumentNullException.ThrowIfNull(store);
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLength);
    string normalized = PageAgentScope.Normalize(path);
    JsonObject document = Envelope(store, normalized, toolNames, liveSurfaceJson);
    foreach (Type stateType in PageAgentScope.StateTypesFor(normalized))
    {
      if (store.GetState(stateType) is IAgentPageFacts facts)
      {
        facts.AddAgentPageFacts(document, normalized);
      }
    }

    return Bound(document, maxLength);
  }

  private static JsonObject Envelope
  (
    IStore store,
    string path,
    IReadOnlyList<string>? toolNames,
    string? liveSurfaceJson
  )
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
      ["tools"] = ToolNames(toolNames),
      ["references"] = References(store),
      ["tags"] = AskTagPayload.Json(store.GetState<AskTagState>().SentFor(store.GetState<AgentSurfaceState>().ConversationGeneration)),
      ["screenshot"] = JsonNode.Parse("null"),
    };
    MergeSurface(document, SurfaceFor(store.GetState<AgentSurfaceState>(), path, liveSurfaceJson));
    return document;
  }

  private static JsonArray References(IStore store)
  {
    int generation = store.GetState<AgentSurfaceState>().ConversationGeneration;
    JsonArray references = [];
    foreach (ResourceReferenceItem item in store.GetState<AskReferenceState>().ChosenFor(generation))
    {
      references.Add(new JsonObject
      {
        ["token"] = item.Token,
        ["type"] = item.Prefix,
        ["idType"] = item.IdTypeName,
        ["id"] = item.Id,
        ["label"] = item.Label.Length <= ReferenceLabelCap ? item.Label : item.Label[..ReferenceLabelCap],
      });
    }

    return references;
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

  private static JsonArray ToolNames(IReadOnlyList<string>? toolNames)
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
    names.Add(AgentSiteMap.ToolName);
    names.Add(ToolName);
    return names;
  }

  private static string? SurfaceFor(AgentSurfaceState surface, string path, string? liveSurfaceJson)
  {
    if (!string.IsNullOrWhiteSpace(liveSurfaceJson))
    {
      return liveSurfaceJson;
    }

    return string.Equals(surface.PageSurfacePath, path, StringComparison.OrdinalIgnoreCase)
      ? surface.PageSurfaceJson
      : null;
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

    document["headings"] = Texts(surface["headings"], HeadingCap, HeadingTextCap);
    document["buttons"] = Texts(surface["buttons"], ButtonCap, ButtonTextCap);
    document["forms"] = Forms(surface["forms"]);
    document["items"] = Items(surface["items"]);
    document["summary"] = Clip(Text(surface["summary"]), SummaryCap);
  }

  private static JsonArray Texts(JsonNode? source, int cap, int textCap)
  {
    JsonArray copy = [];
    if (source is not JsonArray values)
    {
      return copy;
    }

    foreach (JsonNode? value in values)
    {
      if (copy.Count == cap)
      {
        break;
      }

      string text = Clip(Text(value), textCap);
      if (text.Length > 0)
      {
        copy.Add(text);
      }
    }

    return copy;
  }

  private static JsonArray Forms(JsonNode? source)
  {
    JsonArray copy = [];
    if (source is not JsonArray forms)
    {
      return copy;
    }

    foreach (JsonNode? form in forms)
    {
      if (copy.Count == FormCap)
      {
        break;
      }

      if (form is JsonObject value)
      {
        copy.Add
        (
          new JsonObject
          {
            ["name"] = Clip(Text(value["name"]), FormNameCap),
            ["fields"] = Texts(value["fields"], FieldCap, FieldTextCap),
          }
        );
      }
    }

    return copy;
  }

  private static JsonArray Items(JsonNode? source)
  {
    JsonArray copy = [];
    if (source is not JsonArray items)
    {
      return copy;
    }

    foreach (JsonNode? item in items)
    {
      if (copy.Count == ItemCap)
      {
        break;
      }

      if (item is JsonObject value)
      {
        copy.Add
        (
          new JsonObject
          {
            ["id"] = Clip(Text(value["id"]), ItemIdCap),
            ["text"] = Clip(Text(value["text"]), ItemTextCap),
          }
        );
      }
    }

    return copy;
  }

  private static string Text(JsonNode? node) =>
    node is JsonValue value && value.TryGetValue(out string? text) && text is not null ? text : "";

  private static string Clip(string text, int cap) => text.Length <= cap ? text : text[..cap];

  private static string Bound(JsonObject document, int maxLength)
  {
    string json = document.ToJsonString();
    foreach (string name in (string[])["summary", "items", "forms", "buttons", "headings"])
    {
      if (json.Length <= maxLength)
      {
        return json;
      }

      document[name] = name == "summary" ? "" : new JsonArray();
      json = document.ToJsonString();
    }

    return json;
  }
}
