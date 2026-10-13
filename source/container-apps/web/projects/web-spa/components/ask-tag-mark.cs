#region Purpose
// The attributes that make an element pickable by Ask's Tag (task 346), and the kinds it names.
#endregion

#region Design
// Pickable means marked, not every element: ask-tag.ts only offers elements that carry
// data-ask-tag (the kind shown on the chip) inside the page body. The mark is emitted by the
// shared components, not written by hand on each page: Card (and so every Section) marks itself
// with its title, MessageBars marks each bar with its intent and title. A page marks a row
// that stands for a referenceable resource with Resource, which adds data-ask-tag-ref, the 345
// @prefix:id token; the AskTagState Choose handler re-checks that prefix against the generated
// ResourceReferenceRegistry and the user's list policy, so a stale or forbidden prefix never
// reaches the model. data-ask-tag-label overrides the visible-text label the chip shows.
// data-ask-tag-private on any element hides it and everything inside it from pick mode (use it
// on a region that shows secrets). AuthorizeView content the user cannot see is never rendered,
// so it carries no mark and cannot be picked.
#endregion

namespace TimeWarp.Architecture.Components;

/// <summary>Builds the data-ask-tag attributes the Ask Tag pick mode looks for.</summary>
public static class AskTagMark
{
  public const string KindAttribute = "data-ask-tag";

  public const string LabelAttribute = "data-ask-tag-label";

  public const string ReferenceAttribute = "data-ask-tag-ref";

  public const string PrivateAttribute = "data-ask-tag-private";

  public const string CardKind = "Card";

  public const string FeedbackRowKind = "Feedback row";

  public const string FieldKind = "Field";

  /// <summary>Message bar kind for a bar of <paramref name="intent"/>: "Error bar", "Warning bar", …</summary>
  public static string MessageBarKind(MessageBarIntent intent) => intent switch
  {
    MessageBarIntent.Error => "Error bar",
    MessageBarIntent.Warning => "Warning bar",
    MessageBarIntent.Success => "Success bar",
    MessageBarIntent.Info => "Message bar",
    MessageBarIntent.Custom => "Message bar",
    _ => "Message bar",
  };

  /// <summary>Marks an element as pickable with <paramref name="kind"/> and an optional chip label.</summary>
  public static IReadOnlyDictionary<string, object> For(string kind, string? label = null)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(kind);
    Dictionary<string, object> attributes = new() { [KindAttribute] = kind };
    if (!string.IsNullOrWhiteSpace(label))
    {
      attributes[LabelAttribute] = label;
    }

    return attributes;
  }

  /// <summary>Marks an element that stands for a referenceable resource (<c>@prefix:id</c>, task 345).</summary>
  public static IReadOnlyDictionary<string, object> Resource(string kind, string prefix, string id, string? label)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
    ArgumentException.ThrowIfNullOrWhiteSpace(id);
    Dictionary<string, object> attributes = new(For(kind, label))
    {
      [ReferenceAttribute] = "@" + prefix + ":" + id,
    };
    return attributes;
  }
}
