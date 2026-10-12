#region Purpose
// Closed set of triage states an administrator moves a feedback item through.
#endregion

#region Design
// Task 314 decision 1: the de-facto set across feedback boards. Filing sets New; nothing can
// move an item back to New, because New means "nobody has looked yet". Completed and Declined
// are end states that can be reopened. Names match the contracts enum member-for-member and
// handlers map by name, so a reordered contract enum cannot change an item's status. The
// email-worthy subset (Planned, Completed, Declined) is the contracts-side FeedbackStatusRules.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Domain;

public enum FeedbackStatus
{
  New = 0,
  UnderReview = 1,
  Planned = 2,
  InProgress = 3,
  Completed = 4,
  Declined = 5,
}
