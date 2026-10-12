#region Purpose
// Admin contract: list every principal's feedback, filtered and paged.
#endregion

#region Design
// Task 314. admin.feedback.read on the human session schemes. Closed items (Completed,
// Declined) are hidden unless IncludeClosed is true or Status names one explicitly. FiledFrom
// is inclusive and FiledTo exclusive. Page is 1-based; PageSize defaults to 25 and is capped at
// MaxPageSize. GET carries its filters in the query string (IQueryStringRouteProvider), so the
// client builds the URL from the contract. Items omit the body; opening an item loads it.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

using System.Globalization;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.AdminFeedbackRead,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class ListFeedback
{
  public const int DefaultPageSize = 25;
  public const int MaxPageSize = 100;

  [ApiRoute("api/admin/feedback", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IQueryStringRouteProvider, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public FeedbackStatus? Status { get; set; }
    public FeedbackKind? Kind { get; set; }
    public DateTimeOffset? FiledFrom { get; set; }
    public DateTimeOffset? FiledTo { get; set; }
    public bool IncludeClosed { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = DefaultPageSize;

    public string GetRouteWithQueryString()
    {
      var parameters = new NameValueCollection
      {
        { nameof(Status), Status?.ToString() },
        { nameof(Kind), Kind?.ToString() },
        { nameof(FiledFrom), FiledFrom?.ToString("O", CultureInfo.InvariantCulture) },
        { nameof(FiledTo), FiledTo?.ToString("O", CultureInfo.InvariantCulture) },
        { nameof(IncludeClosed), IncludeClosed ? "true" : null },
        { nameof(Page), Page.ToString(CultureInfo.InvariantCulture) },
        { nameof(PageSize), PageSize.ToString(CultureInfo.InvariantCulture) },
      };
      return $"{GetRoute()}?{this.GetQueryString(parameters)}";
    }
  }

  public sealed class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
      RuleFor(query => query.Status).IsInEnum();
      RuleFor(query => query.Kind).IsInEnum();
      RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
      RuleFor(query => query.PageSize).InclusiveBetween(1, MaxPageSize);
      RuleFor(query => query.FiledTo)
        .GreaterThan(query => query.FiledFrom)
        .When(query => query.FiledFrom is not null && query.FiledTo is not null);
    }
  }

  public sealed class Item
  {
    public Guid FeedbackItemId { get; }
    public Guid OwnerPrincipalId { get; }
    public FeedbackKind Kind { get; }
    public FeedbackStatus Status { get; }
    public string Title { get; }
    public DateTimeOffset FiledAt { get; }

    public Item(
      Guid feedbackItemId,
      Guid ownerPrincipalId,
      FeedbackKind kind,
      FeedbackStatus status,
      string title,
      DateTimeOffset filedAt)
    {
      FeedbackItemId = Guard.Against.NullOrEmpty(feedbackItemId);
      OwnerPrincipalId = Guard.Against.NullOrEmpty(ownerPrincipalId);
      if (!Enum.IsDefined(kind))
      {
        throw new ArgumentException("Kind must be a defined feedback kind.", nameof(kind));
      }

      if (!Enum.IsDefined(status))
      {
        throw new ArgumentException("Status must be a defined feedback status.", nameof(status));
      }

      Kind = kind;
      Status = status;
      Title = Guard.Against.NullOrWhiteSpace(title);
      FiledAt = filedAt;
    }
  }

  public sealed class Response : ListResponse<Item>
  {
    public int Page { get; }
    public int PageSize { get; }

    public Response(int totalCount, Item[] items, int page, int pageSize) : base(totalCount, items)
    {
      Page = page;
      PageSize = pageSize;
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    var owner = Guid.Parse("33333333-3333-3333-3333-333333333333");
    Item[] items =
    [
      new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        owner,
        FeedbackKind.BugReport,
        FeedbackStatus.UnderReview,
        "Mock bug report",
        DateTimeOffset.Parse("2026-10-09T00:00:00Z")),
      new(
        Guid.Parse("55555555-5555-5555-5555-555555555555"),
        owner,
        FeedbackKind.FeatureRequest,
        FeedbackStatus.New,
        "Mock feature request",
        DateTimeOffset.Parse("2026-10-08T00:00:00Z")),
    ];
    return _ => new Response(items.Length, items, page: 1, DefaultPageSize);
  }
}
