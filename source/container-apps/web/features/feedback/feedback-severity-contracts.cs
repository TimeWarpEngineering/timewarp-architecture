#region Purpose
// Wire enum for a feedback item's review severity.
#endregion

#region Design
// PascalCase member names on the wire (ContractSerializationDefaults). Member names match
// Features.Feedback.Domain.FeedbackSeverity; the parity test keeps the two sets aligned because
// contracts do not reference domain. The filer's contracts omit the property. Unset is a real
// list-filter value (the unreviewed queue), distinct from omitting the filter.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

public enum FeedbackSeverity
{
  Unset = 0,
  Informational = 1,
  Low = 2,
  Medium = 3,
  High = 4,
  Critical = 5,
}
