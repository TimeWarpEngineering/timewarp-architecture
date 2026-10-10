#region Purpose
// Binds a raw-body feedback upload to UploadFeedbackAttachment.Command.
#endregion

#region Design
// FastEndpoints model binding is replaced entirely by IRequestBinder. The body is the file.
// X-File-Name is percent-encoded. Content-Type parameters (a charset) are stripped so the
// allow-list comparison sees only the media type.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

using TimeWarp.Foundation.Features;

/// <summary>Reads the upload body, file name header, and content type.</summary>
public sealed class UploadFeedbackAttachmentBinder : IRequestBinder<UploadFeedbackAttachment.Command>
{
  /// <inheritdoc />
  public ValueTask<UploadFeedbackAttachment.Command> BindAsync(BinderContext context, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    HttpRequest httpRequest = context.HttpContext.Request;
    string rawName = httpRequest.Headers[FileUploadHeaders.FileName].ToString();
    string fileName = rawName.Length == 0 ? string.Empty : Uri.UnescapeDataString(rawName);
    string contentType = httpRequest.ContentType ?? string.Empty;
    int semicolon = contentType.IndexOf(';', StringComparison.Ordinal);
    if (semicolon >= 0)
    {
      contentType = contentType[..semicolon];
    }

    UploadFeedbackAttachment.Command command = new()
    {
      FileName = fileName,
      ContentType = contentType.Trim(),
      Content = httpRequest.Body,
    };
    return ValueTask.FromResult(command);
  }
}
