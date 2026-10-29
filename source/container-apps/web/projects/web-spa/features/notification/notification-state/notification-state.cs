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
// Copy (task 313): CopyMessage marks the copied row with CopiedMessageId until CopiedUntil
// (now + CopiedConfirmationInterval). NextExpiry includes CopiedUntil, so the same host timer
// that expires Success bars also ends the "Copied" icon swap in RemoveExpired.
// Task 445: page-local bars copy through CopyMessageBar with the MessageBar control's own id,
// so CopiedMessageId may name a bar that is not in the list. RemoveExpired therefore clears it
// only when the confirmation ran out or the copied row itself expired.
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

  /// <summary>How long a row shows its "Copied" confirmation after CopyMessage.</summary>
  public static readonly TimeSpan CopiedConfirmationInterval = TimeSpan.FromSeconds(2);

  private List<NotificationMessage> MessageBarList { get; set; } = [];

  /// <summary>Shell row id or MessageBar control id showing its "Copied" confirmation, or null.</summary>
  public string? CopiedMessageId { get; private set; }

  /// <summary>When the "Copied" confirmation ends, or null when none is showing.</summary>
  public DateTimeOffset? CopiedUntil { get; private set; }

  /// <summary>All messages in arrival order (oldest first).</summary>
  public IReadOnlyList<NotificationMessage> Messages => MessageBarList;

  /// <summary>The newest <see cref="MaxVisible"/> messages, in arrival order.</summary>
  public IReadOnlyList<NotificationMessage> VisibleMessages =>
    MessageBarList.Count <= MaxVisible
      ? MessageBarList
      : MessageBarList.GetRange(MessageBarList.Count - MaxVisible, MaxVisible);

  /// <summary>Messages beyond the visible cap.</summary>
  public int HiddenCount => Math.Max(0, MessageBarList.Count - MaxVisible);

  /// <summary>Earliest pending auto-dismiss or copied-confirmation end, or null when nothing expires.</summary>
  public DateTimeOffset? NextExpiry
  {
    get
    {
      DateTimeOffset? autoDismiss = MessageBarList.Min(message => message.AutoDismissAt);
      return autoDismiss is null || (CopiedUntil is not null && CopiedUntil < autoDismiss)
        ? CopiedUntil
        : autoDismiss;
    }
  }

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
    ClearCopied();
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
    if (CopiedMessageId == id)
    {
      ClearCopied();
    }
  }

  internal void RemoveExpired(DateTimeOffset now)
  {
    bool copiedRowExpired = MessageBarList.Exists(message =>
      message.Id == CopiedMessageId && message.AutoDismissAt is not null && message.AutoDismissAt <= now);
    MessageBarList.RemoveAll(message => message.AutoDismissAt is not null && message.AutoDismissAt <= now);
    if (copiedRowExpired || (CopiedUntil is not null && CopiedUntil <= now))
    {
      ClearCopied();
    }
  }

  internal void Clear()
  {
    MessageBarList.Clear();
    ClearCopied();
  }

  internal void MarkCopied(string id, DateTimeOffset now)
  {
    CopiedMessageId = id;
    CopiedUntil = now + CopiedConfirmationInterval;
  }

  private void ClearCopied()
  {
    CopiedMessageId = null;
    CopiedUntil = null;
  }
}
