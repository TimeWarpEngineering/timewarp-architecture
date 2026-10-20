#region Purpose
// Admin contract: add an internal note to a feedback item.
#endregion

#region Design
// Task 378. feedback.note on the human session schemes and the agent-token scheme. An internal
// note is a thread entry the filer never sees, and no email is sent. ReplyToFeedback stays on
// admin.feedback.manage and the human schemes. The 4000 literal duplicates
// FeedbackItem.MaxMessageLength.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.FeedbackNote,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession + "," + AuthenticationSchemeNames.AgentToken
)]
public static partial class AddFeedbackNote
{
  public const int MaxBodyLength = 4000;

  [ApiRoute("api/admin/feedback/{FeedbackItemId:guid}/notes", HttpVerb.Post)]
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
    public FeedbackAdminView Item { get; }

    public Response(FeedbackAdminView item)
    {
      Item = Guard.Against.Null(item);
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory() =>
    request => new Response(FeedbackAdminView.Sample(
      request is Command { FeedbackItemId: var id } && id != Guid.Empty
        ? id
        : Guid.Parse("11111111-1111-1111-1111-111111111111")));
}
