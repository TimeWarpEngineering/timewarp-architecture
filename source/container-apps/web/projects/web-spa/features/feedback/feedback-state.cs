#region Purpose
// Client cache of the signed-in filer's feedback list, the open item, and the latest receipt.
#endregion

#region Design
// LastReceipt is the proof a submit landed (id and permalink). List refreshes do not clear it.
// EmailCopyAvailable comes from ListMyFeedback so the form does not read ProfileState.
// DraftAttachments are uploads that have not been filed yet. FilingsLoaded is true only after
// ListMyFeedback succeeds: an empty Items list is also the state before that response.
// ComposerActive and the draft flags are presence only (kind, has title, has body). The draft
// text stays on the form. Initialize clears every field so sign-out cannot leave another
// principal's filings or files on screen.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

[StateAccess]
public sealed partial class FeedbackState : State<FeedbackState>
{
  public SubmitFeedback.Response? LastReceipt { get; private set; }
  public IReadOnlyList<ListMyFeedback.Item> Items { get; private set; } = [];
  public bool FilingsLoaded { get; private set; }
  public bool EmailCopyAvailable { get; private set; }
  public GetFeedback.Response? Current { get; private set; }
  public string? LoadError { get; private set; }
  public IReadOnlyList<DraftAttachment> DraftAttachments { get; private set; } = [];
  public bool ComposerActive { get; private set; }
  public string DraftKind { get; private set; } = "";
  public bool DraftHasTitle { get; private set; }
  public bool DraftHasBody { get; private set; }

  /// <summary>One uploaded file that has not been filed yet.</summary>
  public sealed record DraftAttachment(Guid AttachmentId, string FileName, string ContentType);

  public override void Initialize()
  {
    LastReceipt = null;
    Items = [];
    FilingsLoaded = false;
    EmailCopyAvailable = false;
    Current = null;
    LoadError = null;
    DraftAttachments = [];
    ComposerActive = false;
    DraftKind = "";
    DraftHasTitle = false;
    DraftHasBody = false;
  }
}
