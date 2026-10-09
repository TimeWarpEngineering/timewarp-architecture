#region Purpose
// Closed set of feedback kinds the filer chooses.
#endregion

#region Design
// Names match the contracts enum member-for-member. Handlers map by name, not by numeric cast,
// so a reordered contract enum cannot silently file the wrong kind.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Domain;

public enum FeedbackKind
{
  BugReport = 0,
  FeatureRequest = 1,
  Complaint = 2,
  Other = 3,
}
