#region Purpose
// Admin contract: reply privately to the filer of a feedback item.
#endregion

#region Design
// Task 314 decision 2. admin.feedback.manage. The reply is a thread entry the filer sees on
// the permalink; it is never public. Every admin reply is also emailed to the filer when the
// profile has an address (decision 3); FilerEmailed reports whether that send completed.
// The 4000 literal duplicates FeedbackItem.MaxMessageLength; contracts must not reference domain.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.AdminFeedbackManage,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class ReplyToFeedback
{
  public const int MaxBodyLength = 4000;

  [ApiRoute("api/admin/feedback/{FeedbackItemId:guid}/replies", HttpVerb.Post)]
  public sealed partial class Command : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
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
    public FeedbackAdminView Item { get; }
    public bool FilerEmailed { get; }

    public Response(FeedbackAdminView item, bool filerEmailed)
    {
      Item = Guard.Against.Null(item);
      FilerEmailed = filerEmailed;
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory() =>
    request => new Response(
      FeedbackAdminView.Sample(
        request is Command { FeedbackItemId: var id } && id != Guid.Empty
          ? id
          : Guid.Parse("11111111-1111-1111-1111-111111111111")),
      filerEmailed: true);
}
