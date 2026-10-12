#region Purpose
// Admin contract: open any principal's feedback item with its full thread.
#endregion

#region Design
// Task 314. admin.feedback.read. Unlike GetFeedback there is no owner check, and the thread
// includes internal notes. A missing id is 404.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.AdminFeedbackRead,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class GetFeedbackForAdmin
{
  [ApiRoute("api/admin/feedback/{FeedbackItemId:guid}", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
      RuleFor(query => query.FeedbackItemId).NotEmpty();
    }
  }

  public sealed class Response : BaseResponse
  {
    public FeedbackAdminView Item { get; }

    public Response(FeedbackAdminView item)
    {
      Item = Guard.Against.Null(item);
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory() =>
    request => new Response(FeedbackAdminView.Sample(
      request is Query { FeedbackItemId: var id } && id != Guid.Empty
        ? id
        : Guid.Parse("11111111-1111-1111-1111-111111111111")));
}
