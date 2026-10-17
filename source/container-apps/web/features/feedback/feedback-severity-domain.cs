#region Purpose
// Closed set of review severities on a feedback item. The filer never sees it.
#endregion

#region Design
// Unset is the filing default and means "not reviewed". Nothing returns an item to Unset:
// setting a severity is the review, and a later change (Low to High) appends another entry.
// Names match the contracts enum member-for-member. Handlers map by name, not by numeric
// cast, so a reordered contract enum cannot change an item's severity. Status and severity
// do not move each other.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Domain;

public enum FeedbackSeverity
{
  Unset = 0,
  Informational = 1,
  Low = 2,
  Medium = 3,
  High = 4,
  Critical = 5,
}
