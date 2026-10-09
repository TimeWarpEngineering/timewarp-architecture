#region Purpose
// Client cache of the signed-in filer's feedback list, the open item, and the latest receipt.
#endregion

#region Design
// LastReceipt is the proof a submit landed (id and permalink). List refreshes do not clear it.
// EmailCopyAvailable comes from ListMyFeedback so the form does not read ProfileState.
// Initialize clears every field so sign-out cannot leave another principal's filings on screen.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

[StateAccess]
public sealed partial class FeedbackState : State<FeedbackState>
{
  public SubmitFeedback.Response? LastReceipt { get; private set; }
  public IReadOnlyList<ListMyFeedback.Item> Items { get; private set; } = [];
  public bool EmailCopyAvailable { get; private set; }
  public GetFeedback.Response? Current { get; private set; }
  public string? LoadError { get; private set; }

  public override void Initialize()
  {
    LastReceipt = null;
    Items = [];
    EmailCopyAvailable = false;
    Current = null;
    LoadError = null;
  }
}
