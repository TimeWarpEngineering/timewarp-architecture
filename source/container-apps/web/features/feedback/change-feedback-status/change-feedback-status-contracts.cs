#region Purpose
// Admin contract: move a feedback item to another status.
#endregion

#region Design
// Task 314 decisions 1 and 3. admin.feedback.manage. New is refused (400): nothing returns an
// item to "nobody has looked yet". Choosing the current status is 409. The change is recorded
// in the thread. FilerEmailed is true only when the new status is one FeedbackStatusRules
// emails about (Planned, Completed, Declined), the filer's profile has an email, and the send
// completed. The response carries the updated admin view.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.AdminFeedbackManage,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class ChangeFeedbackStatus
{
  [ApiRoute("api/admin/feedback/{FeedbackItemId:guid}/status", HttpVerb.Put)]
  public sealed partial class Command : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public FeedbackStatus Status { get; set; }
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(command => command.FeedbackItemId).NotEmpty();
      RuleFor(command => command.Status)
        .IsInEnum()
        .NotEqual(FeedbackStatus.New)
        .WithMessage("An item cannot return to New.");
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
    request =>
    {
      Guid id = request is Command { FeedbackItemId: var routeId } && routeId != Guid.Empty
        ? routeId
        : Guid.Parse("11111111-1111-1111-1111-111111111111");
      FeedbackStatus status = request is Command { Status: var chosen } && chosen != FeedbackStatus.New
        ? chosen
        : FeedbackStatus.UnderReview;
      return new Response(FeedbackAdminView.Sample(id, status), FeedbackStatusRules.NotifiesFiler(status));
    };
}
