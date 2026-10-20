#region Purpose
// Admin contract: move a feedback item to another status.
#endregion

#region Design
// Task 378. feedback.status on the human session schemes and the agent-token scheme. The rules
// are the same as before: New is refused (400), the current status is 409, and the change is
// recorded in the thread. It does not set severity. FilerEmailed is true only when the new
// status is one FeedbackStatusRules emails about (Planned, Completed, Declined), the filer's
// profile has an email, and the send completed — including when the caller is an agent. The
// response carries the updated admin view.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.FeedbackStatus,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession + "," + AuthenticationSchemeNames.AgentToken
)]
public static partial class ChangeFeedbackStatus
{
  [ApiRoute("api/admin/feedback/{FeedbackItemId:guid}/status", HttpVerb.Put)]
  public sealed partial class Command : IApiRequest, ICommand<OneOf<Response, SharedProblemDetails>>
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
