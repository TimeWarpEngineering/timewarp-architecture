#region Purpose
// Wire enum for the kind of one entry in a feedback item's activity thread.
#endregion

#region Design
// Member names match Features.Feedback.Domain.FeedbackActivityKind (parity test). The filer's
// view never receives InternalNote or SeverityChange entries; the server filters them out,
// not the SPA.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

public enum FeedbackActivityKind
{
  StatusChange = 0,
  AdminReply = 1,
  FilerReply = 2,
  InternalNote = 3,
  SeverityChange = 4,
}
