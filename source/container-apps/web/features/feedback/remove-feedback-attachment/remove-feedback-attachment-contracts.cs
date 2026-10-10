#region Purpose
// Contract for removing an attachment that has not been filed yet.
#endregion

#region Design
// Delete carries only the route id, so the generated JSON endpoint is enough. A filed
// attachment returns 409 from the handler and stays on the item. GetMockResponseFactory
// echoes the route id so mock mode can drop the draft row.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.FeedbackFileSelf,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class RemoveFeedbackAttachment
{
  [ApiRoute("api/Feedback/attachments/{AttachmentId:guid}", HttpVerb.Delete)]
  public sealed partial class Command : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(command => command.AttachmentId).NotEmpty();
    }
  }

  public sealed class Response : BaseResponse
  {
    public Guid AttachmentId { get; }

    public Response(Guid attachmentId)
    {
      AttachmentId = Guard.Against.NullOrEmpty(attachmentId);
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory() =>
    request =>
    {
      Guid attachmentId = request is Command { AttachmentId: Guid routeId } && routeId != Guid.Empty
        ? routeId
        : Guid.Empty;
      return new Response(attachmentId == Guid.Empty
        ? Guid.Parse("33333333-3333-3333-3333-333333333333")
        : attachmentId);
    };
}
