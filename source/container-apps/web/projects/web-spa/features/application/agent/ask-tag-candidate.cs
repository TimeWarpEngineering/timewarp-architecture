#region Purpose
// What ask-tag.ts reports for the element the person picked: kind, label, visible text, data-qa,
// landmark, and the optional @prefix:id token.
#endregion

#region Design
// Task 346. The browser's description, not a DOM dump: every field is short visible text or a
// mark the app emitted (AskTagMark). The AskTagState Choose handler does not trust it: it trims
// and caps every field again and re-checks the reference before anything is attached.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>The element chosen in pick mode, as the browser described it.</summary>
public sealed record AskTagCandidate
(
  string Kind,
  string Label,
  string Text,
  string? DataQa,
  string? Landmark,
  string? Reference
);
