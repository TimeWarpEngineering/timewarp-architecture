#region Purpose
// The JSON an Ask message carries for its tagged elements, and the text block it is sent in.
#endregion

#region Design
// Task 346. One object per tag: kind, label, text, dataQa, landmark, route, and, when the element
// stands for a referenceable resource, reference (token, type, idType, id), the same shape as a
// page_context.references entry. Nothing else about the element is sent: no markup, attributes,
// styles, or values of form inputs. AskTagChatClient appends Message to the user message it
// sends; page_context.tags lists the tags already sent in the conversation with the same Json.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using System.Text.Json.Nodes;

/// <summary>Serializes tagged elements for the Ask model.</summary>
public static class AskTagPayload
{
  public const string MessageHeading =
    "Elements I tagged on the page (see page_context.tags for the ones sent earlier):\n";

  public static JsonArray Json(IReadOnlyList<AskTag> tags)
  {
    ArgumentNullException.ThrowIfNull(tags);
    JsonArray array = [];
    foreach (AskTag tag in tags)
    {
      JsonObject entry = new()
      {
        ["kind"] = tag.Kind,
        ["label"] = tag.Label,
        ["text"] = tag.Text,
        ["dataQa"] = tag.DataQa,
        ["landmark"] = tag.Landmark,
        ["route"] = tag.Route,
      };
      if (tag.Reference is { } reference)
      {
        entry["reference"] = new JsonObject
        {
          ["token"] = reference.Token,
          ["type"] = reference.Prefix,
          ["idType"] = reference.IdTypeName,
          ["id"] = reference.Id,
        };
      }

      array.Add(entry);
    }

    return array;
  }

  /// <summary>The text appended to the user message that carries <paramref name="tags"/>.</summary>
  public static string Message(IReadOnlyList<AskTag> tags) => MessageHeading + Json(tags).ToJsonString();
}
