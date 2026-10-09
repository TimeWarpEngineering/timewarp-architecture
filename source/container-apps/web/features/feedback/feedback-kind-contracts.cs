#region Purpose
// Wire enum for a feedback filing's kind.
#endregion

#region Design
// PascalCase member names on the wire (ContractSerializationDefaults). Integers are refused.
// Member names match Features.Feedback.Domain.FeedbackKind. Contracts do not reference domain,
// so the parity test is the check that the two sets stay aligned.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

public enum FeedbackKind
{
  BugReport = 0,
  FeatureRequest = 1,
  Complaint = 2,
  Other = 3,
}
