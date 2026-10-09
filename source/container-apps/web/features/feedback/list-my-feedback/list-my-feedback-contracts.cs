#region Purpose
// Contract for the signed-in filer's own feedback list, plus whether an email copy can be offered.
#endregion

#region Design
// EmailCopyAvailable is true only when the caller's profile already has an email. The list
// handler does not create a profile row. The form uses the flag to show "email me a copy"
// and does not ask the filer to add an address.
// Items omit the body; opening an item loads it.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.FeedbackFileSelf,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class ListMyFeedback
{
  [ApiRoute("api/Feedback", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>;

  public sealed class Item
  {
    public Guid FeedbackItemId { get; }
    public string Permalink { get; }
    public FeedbackKind Kind { get; }
    public string Title { get; }
    public DateTimeOffset FiledAt { get; }

    public Item(
      Guid feedbackItemId,
      string permalink,
      FeedbackKind kind,
      string title,
      DateTimeOffset filedAt)
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
    return _ => new Response(
      [
        new Item(
          feedbackItemId,
          $"/Feedback/{feedbackItemId:D}",
          FeedbackKind.Complaint,
          "Mock complaint",
          DateTimeOffset.Parse("2026-10-09T00:00:00Z"))
      ],
      emailCopyAvailable: false);
  }
}
