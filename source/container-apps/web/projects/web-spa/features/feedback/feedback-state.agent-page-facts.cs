#region Purpose
// page_context facts for the pages that declare feedback actions (the list and one filing).
#endregion

#region Design
// The filings list (first 20: id, title, kind) lets the agent open or reference an item by id.
// Draft facts are presence only; the draft text stays on the form. On one filing's page the path
// ends with its id: openFeedbackId names it, purpose says so, and openItem is added once the
// loaded Current is that item, so a stale Current from another filing never shows.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

using System.Text.Json.Nodes;
using TimeWarp.Architecture.Features.Applications;

partial class FeedbackState : IAgentPageFacts
{
  private const int AgentFilingCap = 20;

  /// <inheritdoc />
  public void AddAgentPageFacts(JsonObject document, string path)
  {
    ArgumentNullException.ThrowIfNull(document);
    ArgumentNullException.ThrowIfNull(path);
    document["page"] = "Feedback";
    JsonArray filings = [];
    foreach (ListMyFeedback.Item item in Items.Take(AgentFilingCap))
    {
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

    document["filingsLoaded"] = FilingsLoaded;
    document["filingCount"] = Items.Count;
    document["filings"] = filings;
    document["emailCopyAvailable"] = EmailCopyAvailable;
    document["draftAttachmentCount"] = DraftAttachments.Count;
    if (ComposerActive)
    {
      document["draft"] = new JsonObject
      {
        ["kind"] = DraftKind,
        ["hasTitle"] = DraftHasTitle,
        ["hasBody"] = DraftHasBody,
      };
    }

    string last = path[(path.LastIndexOf('/', StringComparison.Ordinal) + 1)..];
    if (!Guid.TryParse(last, out Guid openId))
    {
      return;
    }

    document["purpose"] = "One feedback item you filed.";
    document["openFeedbackId"] = openId.ToString("D");
    if (Current is not null && Current.FeedbackItemId == openId)
    {
      document["openItem"] = new JsonObject
      {
        ["id"] = Current.FeedbackItemId.ToString("D"),
        ["title"] = Current.Title,
        ["kind"] = Current.Kind.ToString(),
      };
    }
  }
}
