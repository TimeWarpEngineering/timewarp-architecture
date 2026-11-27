#region Purpose
// Contract for uploading one feedback attachment before the item is filed.
#endregion

#region Design
// The body is the file (IFileUploadRequest), not JSON. There is no [ApiEndpoint]: the
// generated FastEndpoint always writes JSON and cannot bind a request stream. A hand-written
// BaseFastEndpoint serves the route, which is what TWA0006 coverage requires.
// The file name arrives in X-File-Name. Size and magic checks run in the handler because
// validation must not consume the stream. GetMockResponseFactory keeps mock mode off the
// real store; the id is new on each call.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

[EndpointAuthorize
(
  Policy = PermissionIds.FeedbackFileSelf,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class UploadFeedbackAttachment
{
  [ApiRoute("api/feedback/attachments", HttpVerb.Post)]
  [Operation("feedback.upload-attachment", Description = "Upload a pending attachment for a feedback filing.", Group = "feedback")]
  [NotAggregate("Stores a pending FeedbackAttachment; no FeedbackItem exists until submit links it.")]
  public sealed partial class Command
    : IFileUploadRequest, ICommand<OneOf<Response, SharedProblemDetails>>
  {
    /// <summary>The file's bytes.</summary>
    public Stream Content { get; set; } = Stream.Null;
    /// <summary>The file's name.</summary>
    public string FileName { get; set; } = null!;
    /// <summary>The file's media type. Must be an allowed attachment type.</summary>
    public string ContentType { get; set; } = null!;
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(command => command.FileName).NotEmpty().MaximumLength(FeedbackAttachmentRules.MaxFileNameLength);
      RuleFor(command => command.ContentType)
        .NotEmpty()
        .Must(contentType => FeedbackAttachmentRules.CanonicalContentType(contentType) is not null);
    }
  }

  public sealed class Response : BaseResponse
  {
    /// <summary>The identifier of the uploaded, not yet linked, attachment.</summary>
    public Guid AttachmentId { get; }
    /// <summary>The attachment's normalized file name.</summary>
    public string FileName { get; }
    /// <summary>The attachment's media type.</summary>
    public string ContentType { get; }
    /// <summary>The attachment's size in bytes.</summary>
    public long Size { get; }
    /// <summary>App-rooted path the attachment is downloaded from.</summary>
    public string DownloadPath { get; }

    public Response(
      Guid attachmentId,
      string fileName,
      string contentType,
      long size,
      string downloadPath)
    {
      AttachmentId = Guard.Against.NullOrEmpty(attachmentId);
      FileName = Guard.Against.NullOrWhiteSpace(fileName);
      ContentType = Guard.Against.NullOrWhiteSpace(contentType);
      ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

      Size = size;
      DownloadPath = Guard.Against.NullOrWhiteSpace(downloadPath);
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory() =>
    request =>
    {
      string fileName = request is Command { FileName: { Length: > 0 } name } ? name : "file";
      string contentType = request is Command command
        && FeedbackAttachmentRules.CanonicalContentType(command.ContentType) is string canonical
          ? canonical
          : FeedbackAttachmentRules.Png;
      var attachmentId = Guid.NewGuid();
      return new Response(
        attachmentId,
        fileName,
        contentType,
        size: 1,
        DownloadFeedbackAttachment.DownloadPath(attachmentId));
    };
}
