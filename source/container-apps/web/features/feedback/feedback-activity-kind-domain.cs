#region Purpose
// Kinds of entry in a feedback item's activity thread.
#endregion

#region Design
// Task 314 decisions 1, 2 and 5. Who wrote a reply is part of the kind (AdminReply vs
// FilerReply) so visibility and author role cannot disagree. InternalNote is the only kind the
// filer never sees. Names match the contracts enum; handlers map by name.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Domain;

public enum FeedbackActivityKind
{
  StatusChange = 0,
  AdminReply = 1,
  FilerReply = 2,
  InternalNote = 3,
}
