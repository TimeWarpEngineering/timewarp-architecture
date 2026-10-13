#region Purpose
// One element attached to an Ask message (a composer chip): the semantic description that is sent.
#endregion

#region Design
// Task 346. Strings only, so store state clones it as a plain record. ChipText is exactly what
// the chip shows ("Feedback row: Dark theme broken"); the payload adds the visible text, data-qa,
// landmark and route (AskTagPayload). Reference is set only when the element stands for a
// referenceable resource the user may list: it carries the same token, type, idType and id as a
// 345 @ reference, so a picked row is as precise as typing @prefix:id.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>An element the person tagged for the Ask model to consider.</summary>
public sealed record AskTag
(
  int Id,
  string Kind,
  string Label,
  string Text,
  string? DataQa,
  string? Landmark,
  string Route,
  ResourceReferenceItem? Reference
)
{
  /// <summary>The chip label: <c>Kind: Label</c>, or the kind alone when there is no label.</summary>
  public string ChipText => Label.Length == 0 ? Kind : Kind + ": " + Label;
}
