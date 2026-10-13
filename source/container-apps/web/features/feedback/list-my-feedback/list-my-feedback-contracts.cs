#region Purpose
// Contract for the signed-in filer's own feedback list, plus whether an email copy can be offered.
#endregion

#region Design
// EmailCopyAvailable is true only when the caller's profile already has an email. The list
// handler does not create a profile row. The form uses the flag to show "email me a copy"
// and does not ask the filer to add an address.
// Items omit the body; opening an item loads it. Status (task 314) is the item's current
// triage status, so the filer's list shows where each filing stands.
// Task 345: Search keeps the filer's items whose title contains the term (case-insensitive);
// the query string carries it, and [ReferenceableResource] makes this the "my-feedback" @ type
// in Ask, so a filer can reference their own filings and nothing else.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.FeedbackFileSelf,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
[ReferenceableResource(ListMyFeedback.ReferencePrefix, "My feedback", IdProperty = nameof(ListMyFeedback.Item.FeedbackItemId), LabelProperty = nameof(ListMyFeedback.Item.Title))]
public static partial class ListMyFeedback
{
  /// <summary>The @ prefix of the caller's own feedback item (task 345); pages mark rows with it (AskTagMark, task 346).</summary>
  public const string ReferencePrefix = "my-feedback";

  public const int MaxSearchLength = 200;

  [ApiRoute("api/Feedback", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IQueryStringRouteProvider, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public string? Search { get; set; }

    public string GetRouteWithQueryString()
    {
      if (string.IsNullOrWhiteSpace(Search))
      {
        return GetRoute();
      }

      var parameters = new NameValueCollection
      {
        { nameof(Search), Search.Trim() },
      };
      return $"{GetRoute()}?{this.GetQueryString(parameters)}";
    }
  }

  public sealed class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
      RuleFor(query => query.Search).MaximumLength(MaxSearchLength);
    }
  }

  public sealed class Item : ICloneable
  {
    /// <summary>Immutable (get-only members), so a State clone can share the instance.</summary>
    public object Clone() => this;

    public Guid FeedbackItemId { get; }
    public string Permalink { get; }
    public FeedbackKind Kind { get; }
    public string Title { get; }
    public DateTimeOffset FiledAt { get; }
    public FeedbackStatus Status { get; }

    public Item(
      Guid feedbackItemId,
      string permalink,
      FeedbackKind kind,
      string title,
      DateTimeOffset filedAt,
      FeedbackStatus status = FeedbackStatus.New)
    {
      FeedbackItemId = Guard.Against.NullOrEmpty(feedbackItemId);
      Permalink = Guard.Against.NullOrWhiteSpace(permalink);
      if (!Enum.IsDefined(kind))
      {
        throw new ArgumentException("Kind must be a defined feedback kind.", nameof(kind));
      }

      Kind = kind;
      Title = Guard.Against.NullOrWhiteSpace(title);
      FiledAt = filedAt;
      if (!Enum.IsDefined(status))
      {
        throw new ArgumentException("Status must be a defined feedback status.", nameof(status));
      }

      Status = status;
    }
  }

  public sealed class Response : BaseResponse
  {
    public IReadOnlyList<Item> Items { get; }
    public bool EmailCopyAvailable { get; }

    public Response(IReadOnlyList<Item> items, bool emailCopyAvailable)
    {
      Items = Guard.Against.Null(items);
      EmailCopyAvailable = emailCopyAvailable;
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    var feedbackItemId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    Item[] items =
    [
      new Item(
        feedbackItemId,
        $"/Feedback/{feedbackItemId:D}",
        FeedbackKind.Complaint,
        "Mock complaint",
        DateTimeOffset.Parse("2026-10-09T00:00:00Z"),
        FeedbackStatus.UnderReview)
    ];
    return request =>
    {
      string? search = (request as Query)?.Search?.Trim();
      Item[] matches = string.IsNullOrEmpty(search)
        ? items
        : [.. items.Where(item => item.Title.Contains(search, StringComparison.OrdinalIgnoreCase))];
      return new Response(matches, emailCopyAvailable: false);
    };
  }
}
