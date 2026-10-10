#region Purpose
// Contract for filing one feedback item and receiving its id and permalink.
#endregion

#region Design
// The server derives the owner from the authenticated principal. The command carries no UserId
// (IAuthApiRequest is a mock-mode signal this endpoint does not need).
// Length literals 200 and 8000 duplicate FeedbackItem.MaxTitleLength / MaxBodyLength. Contracts
// must not reference domain.
// Permalink in the response is the relative path /Feedback/{guid:D}. EmailCopySent is true only
// when a copy was actually handed to the sender. AttachmentIds are uploads already stored for
// this principal; the handler links them. The response echoes the ids that were linked.
// GetMockResponseFactory keeps mock mode deterministic with a fixed id.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.FeedbackFileSelf,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class SubmitFeedback
{
  public const int MaxTitleLength = 200;
  public const int MaxBodyLength = 8000;

  [ApiRoute("api/Feedback", HttpVerb.Post)]
  public sealed partial class Command : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public FeedbackKind Kind { get; set; }
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public bool EmailCopy { get; set; }
    public List<Guid> AttachmentIds { get; set; } = [];
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(command => command.Kind).IsInEnum();
      RuleFor(command => command.Title).NotEmpty().MaximumLength(MaxTitleLength);
      RuleFor(command => command.Body).NotEmpty().MaximumLength(MaxBodyLength);
      RuleFor(command => command.AttachmentIds)
        .Cascade(CascadeMode.Stop)
        .NotNull()
        .Must(ids => ids.Count <= FeedbackAttachmentRules.MaxPerItem)
        .Must(ids => ids.Distinct().Count() == ids.Count);
    }
  }

  public sealed class Response : BaseResponse
  {
    public Guid FeedbackItemId { get; }
    public string Permalink { get; }
    public FeedbackKind Kind { get; }
    public string Title { get; }
    public string Body { get; }
    public bool EmailCopySent { get; }
    public IReadOnlyList<Guid> AttachmentIds { get; }

    public Response(
      Guid feedbackItemId,
      string permalink,
      FeedbackKind kind,
      string title,
      string body,
      bool emailCopySent,
      IReadOnlyList<Guid>? attachmentIds = null)
    {
      FeedbackItemId = Guard.Against.NullOrEmpty(feedbackItemId);
      Permalink = Guard.Against.NullOrWhiteSpace(permalink);
      if (!Enum.IsDefined(kind))
      {
        throw new ArgumentException("Kind must be a defined feedback kind.", nameof(kind));
      }

      Kind = kind;
      Title = Guard.Against.NullOrWhiteSpace(title);
      Body = Guard.Against.NullOrWhiteSpace(body);
      EmailCopySent = emailCopySent;
      AttachmentIds = attachmentIds ?? [];
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    var feedbackItemId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    return _ => new Response(
      feedbackItemId,
      $"/Feedback/{feedbackItemId:D}",
      FeedbackKind.Complaint,
      "Mock complaint",
      "Mock body",
      emailCopySent: false);
  }
}
