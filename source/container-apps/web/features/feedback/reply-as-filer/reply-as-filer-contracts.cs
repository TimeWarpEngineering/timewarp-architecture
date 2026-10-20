#region Purpose
// Filer contract: answer in the thread of one of the caller's own feedback items.
#endregion

#region Design
// Task 314 decision 2. feedback.file.self, like the other filing endpoints. Another principal's
// item and a missing id are the same 404. The response carries the item's current status and
// the filer-visible thread (no internal notes, no admin identities), so the permalink page
// renders the server's state after the write. The 4000 literal duplicates
// FeedbackItem.MaxMessageLength.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.FeedbackFileSelf,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class ReplyAsFiler
{
  public const int MaxBodyLength = 4000;

  [ApiRoute("api/Feedback/{FeedbackItemId:guid}/replies", HttpVerb.Post)]
  public sealed partial class Command : IApiRequest, ICommand<OneOf<Response, SharedProblemDetails>>
  {
    public string Body { get; set; } = null!;
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(command => command.FeedbackItemId).NotEmpty();
      RuleFor(command => command.Body).NotEmpty().MaximumLength(MaxBodyLength);
    }
  }

  public sealed class Response : BaseResponse
  {
    public Guid FeedbackItemId { get; }
    public FeedbackStatus Status { get; }
    public IReadOnlyList<FeedbackActivityEntry> Activity { get; }

    public Response(Guid feedbackItemId, FeedbackStatus status, IReadOnlyList<FeedbackActivityEntry>? activity = null)
    {
      FeedbackItemId = Guard.Against.NullOrEmpty(feedbackItemId);
      if (!Enum.IsDefined(status))
      {
        throw new ArgumentException("Status must be a defined feedback status.", nameof(status));
      }

      Status = status;
      Activity = activity ?? [];
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory() =>
    request =>
    {
      Guid id = request is Command { FeedbackItemId: var routeId } && routeId != Guid.Empty
        ? routeId
        : Guid.Parse("11111111-1111-1111-1111-111111111111");
      string body = request is Command { Body: { Length: > 0 } text } ? text : "Mock answer";
      return new Response(
        id,
        FeedbackStatus.UnderReview,
        [new FeedbackActivityEntry(1, FeedbackActivityKind.FilerReply, null, body, null, null, DateTimeOffset.Parse("2026-10-09T01:00:00Z"))]);
    };
}
