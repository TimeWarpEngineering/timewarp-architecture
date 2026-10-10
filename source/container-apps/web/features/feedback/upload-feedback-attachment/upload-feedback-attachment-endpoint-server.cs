#region Purpose
// HTTP endpoint for a raw-body feedback attachment upload.
#endregion

#region Design
// Hand-written BaseFastEndpoint, not [ApiEndpoint]: the generator's success path is JSON
// and its binder cannot take the request stream. TWA0006 still sees this subclass.
// Auth matches the contract: feedback.file.self on both human session schemes.
// MaxRequestBodySize is one byte over the handler's cap, so a body that is just too large
// reaches the handler and gets its 413 problem. Kestrel still stops a larger body (or a
// Content-Length over the limit) on the first read with BadHttpRequestException; HandleAsync
// turns that 413 into the same application/problem+json response instead of an unhandled error.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

using TimeWarp.Architecture.Features.Feedback.Application;
using TimeWarp.Foundation.Features;

/// <summary>POST api/Feedback/attachments</summary>
public sealed class UploadFeedbackAttachmentEndpoint
  : BaseFastEndpoint<UploadFeedbackAttachment.Command, UploadFeedbackAttachment.Response>
{
  /// <inheritdoc />
  public override void Configure()
  {
    Post(UploadFeedbackAttachment.Command.RouteTemplate);
    AuthSchemes(AuthenticationSchemeNames.IdentitySession, AuthenticationSchemeNames.MockIdentitySession);
    Policies(PermissionIds.FeedbackFileSelf);
    RequestBinder(new UploadFeedbackAttachmentBinder());
    MaxRequestBodySize(FeedbackAttachmentRules.MaxBytes + 1L);
  }

  /// <inheritdoc />
  public override async Task HandleAsync(UploadFeedbackAttachment.Command request, CancellationToken cancellationToken)
  {
    try
    {
      await base.HandleAsync(request, cancellationToken).ConfigureAwait(false);
    }
    catch (BadHttpRequestException exception)
      when (exception.StatusCode == StatusCodes.Status413PayloadTooLarge && !HttpContext.Response.HasStarted)
    {
      SharedProblemDetails problem = FeedbackAttachmentHttp.TooLarge();
      HttpContext.Response.ContentType = "application/problem+json; charset=utf-8";
      HttpContext.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
      await HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken).ConfigureAwait(false);
    }
  }
}
