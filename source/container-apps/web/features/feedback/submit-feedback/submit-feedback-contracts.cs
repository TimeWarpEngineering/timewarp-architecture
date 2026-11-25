#region Purpose
// Contract for filing one feedback item and receiving its id and permalink.
#endregion

#region Design
// The server derives the owner from the authenticated principal. The command does not name its
// caller.
// Kind, Title and Body are IFeedbackItemDetails (task 475), whose validator this one includes and
// whose length literals duplicate FeedbackItem.MaxTitleLength / MaxBodyLength (contracts must not
// reference domain). MaxTitleLength / MaxBodyLength here forward to it for existing callers.
// Permalink in the response is the relative path /Feedback/{guid:D}. EmailCopySent is true only
// when a copy was actually handed to the sender. AttachmentIds are uploads already stored for
// this principal; the handler links them. The response echoes the ids that were linked.
// When some ids cannot be filed (expired after FeedbackAttachmentRules.PendingLifetime,
// removed, or filed elsewhere), the 400 problem lists them under the
// UnavailableAttachmentIdsExtension key so the client can drop exactly those from its draft.
// Task 521: PagePath is the app path the filer was on when they opened the form. It is optional
// (null or empty files without it). The validator refuses anything that is not app-relative:
// it must start with exactly one '/', and a query string, fragment, backslash, whitespace or
// control character is refused rather than stripped, so a query value (a token, an email) can
// never reach storage and an absolute or protocol-relative URL is never stored as a page.
// IsPagePath is the rule; the client calls it to drop a path the server would refuse.
// FeedbackItem.IsAppRelativePath is the domain copy and MaxPagePathLength duplicates
// FeedbackItem.MaxPagePathLength (a test locks both). The app version is not on the command:
// the server reads its own.
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
  public const int MaxTitleLength = IFeedbackItemDetails.MaxTitleLength;
  public const int MaxBodyLength = IFeedbackItemDetails.MaxBodyLength;
  public const int MaxPagePathLength = 512;

  /// <summary>Problem extension key listing the attachment ids that could not be filed.</summary>
  public const string UnavailableAttachmentIdsExtension = "unavailableAttachmentIds";

  [ApiRoute("api/feedback", HttpVerb.Post)]
  [Operation("feedback.submit", Description = "File a feedback item for the current user.", Group = "feedback")]
  public sealed partial class Command
    : IApiRequest, IFeedbackItemDetails, ICommand<OneOf<Response, SharedProblemDetails>>
  {
    public FeedbackKind Kind { get; set; }
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public bool EmailCopy { get; set; }
    public List<Guid> AttachmentIds { get; set; } = [];
    /// <summary>The app path the filer was on (path only, no query string or fragment). Optional.</summary>
    public string? PagePath { get; set; }
  }

  /// <summary>
  /// True when <paramref name="path"/> starts with exactly one '/' and holds no query, fragment,
  /// backslash, whitespace or control character, within <see cref="MaxPagePathLength"/>.
  /// </summary>
  public static bool IsPagePath(string? path)
  {
    if (string.IsNullOrEmpty(path) || path.Length > MaxPagePathLength || path[0] != '/'
      || (path.Length > 1 && path[1] == '/'))
    {
      return false;
    }

    foreach (char character in path)
    {
      if (character is '?' or '#' or '\\' || char.IsWhiteSpace(character) || char.IsControl(character))
      {
        return false;
      }
    }

    return true;
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      Include(new FeedbackItemDetailsValidator());
      RuleFor(command => command.AttachmentIds)
        .Cascade(CascadeMode.Stop)
        .NotNull()
        .Must(ids => ids.Count <= FeedbackAttachmentRules.MaxPerItem)
        .Must(ids => ids.Distinct().Count() == ids.Count);
      RuleFor(command => command.PagePath)
        .Must(IsPagePath)
        .When(command => !string.IsNullOrEmpty(command.PagePath))
        .WithMessage("Page path must be an app-relative path (starting with '/') with no query string or fragment, at most 512 characters.");
    }
  }

  public sealed class Response : BaseResponse, ICloneable
  {
    /// <summary>Immutable (get-only members), so a State clone can share the instance.</summary>
    public object Clone() => this;

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
