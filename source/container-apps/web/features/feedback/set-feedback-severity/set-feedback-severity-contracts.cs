#region Purpose
// Admin contract: set a feedback item's review severity.
#endregion

#region Design
// Task 378. feedback.severity on the human session schemes and the agent-token scheme.
// Unset is refused (400): nothing returns an item to "not reviewed". The current severity is
// 409. The change is a filer-invisible SeverityChange entry. It does not move status and sends
// no email. The response is the updated admin view. Administrator holds the permission, so the
// admin page can call it.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.FeedbackSeverity,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession + "," + AuthenticationSchemeNames.AgentToken
)]
public static partial class SetFeedbackSeverity
{
  [ApiRoute("api/admin/feedback/{FeedbackItemId:guid}/severity", HttpVerb.Put)]
  public sealed partial class Command : IApiRequest, ICommand<OneOf<Response, SharedProblemDetails>>
  {
    public FeedbackSeverity Severity { get; set; }
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(command => command.FeedbackItemId).NotEmpty();
      RuleFor(command => command.Severity)
        .IsInEnum()
        .NotEqual(FeedbackSeverity.Unset)
        .WithMessage("An item cannot return to Unset.");
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
    request =>
    {
      Guid id = request is Command { FeedbackItemId: var routeId } && routeId != Guid.Empty
        ? routeId
        : Guid.Parse("11111111-1111-1111-1111-111111111111");
      return new Response(FeedbackAdminView.Sample(id));
    };
}
