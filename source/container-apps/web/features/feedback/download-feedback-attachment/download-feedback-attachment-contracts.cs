#region Purpose
// Contract for downloading one feedback attachment.
#endregion

#region Design
// The browser loads the bytes with img src and anchors, so the endpoint writes the raw
// stream. There is no [ApiEndpoint] because the generated endpoint JSON-serializes success.
// A hand-written BaseFastEndpoint covers TWA0006. DownloadPath prefixes a slash: GetRoute
// is app-relative without one, and a relative api/... link from /Feedback would miss.
// The owner-or-read check is the handler's. The route has no policy. Schemes are the human
// sessions and the agent token, so an anonymous caller never reaches it. A read-only feedback
// admin is not stopped by the filer's feedback.file.self permission (task 314). feedback.read
// (task 378) is the agent's grant and also a human grant. admin.feedback.read stays accepted
// on a human session. An agent token is not evaluated under the human schemes.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

[EndpointAuthorize
(
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession + "," + AuthenticationSchemeNames.AgentToken
)]
public static partial class DownloadFeedbackAttachment
{
  /// <summary>App-rooted path for markdown and img src.</summary>
  public static string DownloadPath(Guid attachmentId) =>
    "/" + new Query { AttachmentId = attachmentId }.GetRoute();

  [ApiRoute("api/Feedback/attachments/{AttachmentId:guid}", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
      RuleFor(query => query.AttachmentId).NotEmpty();
    }
  }

  public sealed class Response : BaseResponse
  {
    public Stream Content { get; }
    public string ContentType { get; }
    public string FileName { get; }

    public Response(Stream content, string contentType, string fileName)
    {
      Content = content ?? throw new ArgumentNullException(nameof(content));
      ContentType = Guard.Against.NullOrWhiteSpace(contentType);
      FileName = Guard.Against.NullOrWhiteSpace(fileName);
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory() =>
    _ => new Response(new MemoryStream("%PDF-1.4"u8.ToArray()), FeedbackAttachmentRules.Pdf, "mock.pdf");
}
