#region Purpose
// SPA state for the admin feedback list, its filter, and the one open item.
#endregion

#region Design
// Items null = no snapshot yet (or the last fetch failed: ListLoadError); empty = loaded with zero rows. Filter fields (Status, Kind,
// IncludeClosed, Severity, Page) and the sort (SortBy, SortDescending, task 327) are State
// actions; the page dispatches FetchFeedbackList afterwards. The sort defaults to FiledAt
// descending. Status null with IncludeClosed false is the server's default "open items" view.
// Severity null is every severity. Unset is the unreviewed queue (task 378).
// Current is the open item. Every mutation stores the returned Item here, so the thread never
// needs a re-fetch. LastReplySucceeded and LastNoteSucceeded are false at request start and true
// in HandleSuccess, so the page clears a draft only after a write landed (DefaultApiHandler does
// not throw on problem details). Initialize clears everything so sign-out leaves nothing behind.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Feedback;

using TimeWarp.Architecture.Features.Feedback;

[StateAccess]
public sealed partial class FeedbackAdminState : State<FeedbackAdminState>
{
  private List<ListFeedback.Item>? ItemList { get; set; }

  public IReadOnlyList<ListFeedback.Item>? Items => ItemList?.AsReadOnly();
  public int TotalCount { get; private set; }
  public int Page { get; private set; } = 1;
  public int PageSize { get; private set; } = ListFeedback.DefaultPageSize;
  public FeedbackStatus? Status { get; private set; }
  public FeedbackKind? Kind { get; private set; }
  public FeedbackSeverity? Severity { get; private set; }
  public bool IncludeClosed { get; private set; }
  public FeedbackListSortColumn SortBy { get; private set; } = FeedbackListSortColumn.FiledAt;
  public bool SortDescending { get; private set; } = true;
  public FeedbackAdminView? Current { get; private set; }
  public string? LoadError { get; private set; }
  public string? ListLoadError { get; private set; }
  public bool LastReplySucceeded { get; private set; }
  public bool LastNoteSucceeded { get; private set; }

  public bool HasPreviousPage => Page > 1;
  public bool HasNextPage => (long)Page * PageSize < TotalCount;

  public override void Initialize()
  {
    ItemList = null;
    TotalCount = 0;
    Page = 1;
    PageSize = ListFeedback.DefaultPageSize;
    Status = null;
    Kind = null;
    Severity = null;
    IncludeClosed = false;
    SortBy = FeedbackListSortColumn.FiledAt;
    SortDescending = true;
    Current = null;
    LoadError = null;
    ListLoadError = null;
    LastReplySucceeded = false;
    LastNoteSucceeded = false;
  }
}
