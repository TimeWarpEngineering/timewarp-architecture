#region Purpose
// Shared shape and validation rules for the filed fields of a feedback item (kind, title, body).
#endregion

#region Design
// SubmitFeedback.Command implements it. Nothing edits a filing (FeedbackItem Design), so there is
// no update command; the interface is the one set of members the filer writes. The
// contract-agreement analyzer (TWA0039, task 475) proves each member exists on FeedbackItem with a
// compatible type, through the wire-enum rule for Kind.
// The read models (GetFeedback, FeedbackAdminView, the list items) are get-only, so they do not
// implement this { get; set; } interface; TWA0041 checks their members against the root instead.
// The length literals duplicate FeedbackItem.MaxTitleLength / MaxBodyLength because contracts must
// not reference domain. SubmitFeedback keeps its MaxTitleLength / MaxBodyLength names for callers.
// SubmitFeedback.Validator uses Include, not SetValidator, so the rules run as if declared there
// and property names are unchanged for the Blazilla form.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

public interface IFeedbackItemDetails
{
  public const int MaxTitleLength = 200;
  public const int MaxBodyLength = 8000;

  public FeedbackKind Kind { get; set; }
  public string Title { get; set; }
  public string Body { get; set; }
}

public sealed class FeedbackItemDetailsValidator : AbstractValidator<IFeedbackItemDetails>
{
  public FeedbackItemDetailsValidator()
  {
    RuleFor(details => details.Kind).IsInEnum();
    RuleFor(details => details.Title).NotEmpty().MaximumLength(IFeedbackItemDetails.MaxTitleLength);
    RuleFor(details => details.Body).NotEmpty().MaximumLength(IFeedbackItemDetails.MaxBodyLength);
  }
}
