#region Purpose
// Root partial for NotificationState: the single notification region every page paints.
#endregion

#region Design
// One region, one owner, one shape (task 247). The shell's MessageBars host renders this
// list directly below the page header and above the first card; pages, cards, and feature
// components never render their own outcome bars (TWA0025). Handlers publish
// ProblemDetailsNotification / OutcomeNotification, components dispatch AddNotification /
// ReportProblem, and the region paints the result. Section and Card have no error slot;
// the shell host is the application surface.
// No INotificationService: it throws FluentServiceProviderException unless a provider is in
// the render tree, and headless SPA tests have no provider.
// Shape: Intent + Title + optional Body. A SharedProblemDetails maps Title→Title and
// Detail→Body (FromProblem); a Body that repeats the Title is dropped. Never "Error" as a
// title and never Title: Detail glued into one string.
// Dedupe: messages are keyed by (Intent, Title, Body). Pushing an identical message replaces
// the existing one (same Id, refreshed expiry) so a pipeline handler and a page reporting the
// same failure yield one bar. The visible stack is capped at MaxVisible; the host offers the
// hidden remainder behind a "+N more" affordance.
// Lifetime: Success bars carry AutoDismissAt (now + SuccessAutoDismissInterval) and the host
// expires them via ExpireMessages; everything else stays until DismissMessage or the route
// changes (NavigationListener → ClearOnNavigation). Initialize clears the list so a reset
// drops stale bars.
// Namespace is Features substrate (not a product slice): DefaultApiHandler and other base
// handlers publish failures here, so every product may depend on it without TWA0009 opt-outs.
#endregion

namespace TimeWarp.Architecture.Features;

[StateAccess]
public sealed partial class NotificationState : State<NotificationState>
{
  /// <summary>Bars painted before the host collapses the rest behind "+N more".</summary>
  public const int MaxVisible = 3;

  /// <summary>How long a Success bar stays before the host expires it.</summary>
  public static readonly TimeSpan SuccessAutoDismissInterval = TimeSpan.FromSeconds(6);

  private List<NotificationMessage> MessageBarList { get; set; } = [];

  /// <summary>All messages in arrival order (oldest first).</summary>
  public IReadOnlyList<NotificationMessage> Messages => MessageBarList;

  /// <summary>The newest <see cref="MaxVisible"/> messages, in arrival order.</summary>
  public IReadOnlyList<NotificationMessage> VisibleMessages =>
    MessageBarList.Count <= MaxVisible
      ? MessageBarList
      : MessageBarList.GetRange(MessageBarList.Count - MaxVisible, MaxVisible);

  /// <summary>Messages beyond the visible cap.</summary>
  public int HiddenCount => Math.Max(0, MessageBarList.Count - MaxVisible);

  /// <summary>Earliest pending auto-dismiss, or null when nothing expires.</summary>
  public DateTimeOffset? NextExpiry =>
    MessageBarList.Min(message => message.AutoDismissAt);

  public sealed record NotificationMessage
  (
    string Id,
    MessageBarIntent Intent,
    string Title,
    string? Body,
    DateTimeOffset? AutoDismissAt
  );

  public NotificationState()
  {
    Initialize();
  }

  public sealed override void Initialize()
  {
    MessageBarList = [];
  }

  /// <summary>Title/Body shape for a problem: Title is the problem title, Body its detail.</summary>
  public static (string Title, string? Body) FromProblem(SharedProblemDetails problem)
  {
    string title = string.IsNullOrWhiteSpace(problem.Title)
      ? problem.Detail ?? "Request failed"
      : problem.Title;
    string? body = string.IsNullOrWhiteSpace(problem.Title) ? null : problem.Detail;
    return (title, body);
  }

  internal void AddMessage(MessageBarIntent intent, string title, string? body, DateTimeOffset now)
  {
    string normalizedTitle = title.Trim();
    string? normalizedBody = string.IsNullOrWhiteSpace(body) ? null : body.Trim();
    if (normalizedBody is not null
      && string.Equals(normalizedBody, normalizedTitle, StringComparison.OrdinalIgnoreCase))
    {
      normalizedBody = null;
    }

    DateTimeOffset? autoDismissAt =
      intent == MessageBarIntent.Success ? now + SuccessAutoDismissInterval : null;

    int existingIndex = MessageBarList.FindIndex(message =>
      message.Intent == intent
      && string.Equals(message.Title, normalizedTitle, StringComparison.Ordinal)
      && string.Equals(message.Body, normalizedBody, StringComparison.Ordinal));

    if (existingIndex >= 0)
    {
      MessageBarList[existingIndex] = MessageBarList[existingIndex] with { AutoDismissAt = autoDismissAt };
      return;
    }

    MessageBarList.Add
    (
      new NotificationMessage
      (
        Guid.NewGuid().ToString("N"),
        intent,
        normalizedTitle,
        normalizedBody,
        autoDismissAt
      )
    );
  }

  internal void RemoveMessage(string id)
  {
    MessageBarList.RemoveAll(message => message.Id == id);
  }

  internal void RemoveExpired(DateTimeOffset now)
  {
    MessageBarList.RemoveAll(message => message.AutoDismissAt is not null && message.AutoDismissAt <= now);
  }

  internal void Clear()
  {
    MessageBarList.Clear();
  }
}
