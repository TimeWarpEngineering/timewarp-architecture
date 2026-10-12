#region Purpose
// Rules over FeedbackStatus shared by the server handlers and the SPA.
#endregion

#region Design
// One copy of each rule (task 314 decisions 1, 3 and 6): the admin picker offers Assignable,
// the admin list hides IsClosed items by default, and the status-change handler emails the
// filer only when NotifiesFiler is true. The domain enforces "never back to New" itself; this
// list is what the SPA offers and must not include New (pinned by a test).
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

/// <summary>Rules over <see cref="FeedbackStatus"/> shared by handlers and the SPA.</summary>
public static class FeedbackStatusRules
{
  /// <summary>Statuses an administrator may choose. Nothing moves an item back to New.</summary>
  public static IReadOnlyList<FeedbackStatus> Assignable { get; } =
  [
    FeedbackStatus.UnderReview,
    FeedbackStatus.Planned,
    FeedbackStatus.InProgress,
    FeedbackStatus.Completed,
    FeedbackStatus.Declined,
  ];

  /// <summary>End states. The admin list hides them unless asked.</summary>
  public static bool IsClosed(FeedbackStatus status) =>
    status is FeedbackStatus.Completed or FeedbackStatus.Declined;

  /// <summary>Changes to these statuses email the filer; the rest are in-app only.</summary>
  public static bool NotifiesFiler(FeedbackStatus status) =>
    status is FeedbackStatus.Planned or FeedbackStatus.Completed or FeedbackStatus.Declined;
}
