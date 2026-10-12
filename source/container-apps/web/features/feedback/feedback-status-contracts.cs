#region Purpose
// Wire enum for a feedback item's triage status.
#endregion

#region Design
// PascalCase member names on the wire (ContractSerializationDefaults). Member names match
// Features.Feedback.Domain.FeedbackStatus; the parity test keeps the two sets aligned because
// contracts do not reference domain. The rules over the set live in FeedbackStatusRules.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

public enum FeedbackStatus
{
  New = 0,
  UnderReview = 1,
  Planned = 2,
  InProgress = 3,
  Completed = 4,
  Declined = 5,
}
