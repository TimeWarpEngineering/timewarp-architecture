#region Purpose
// HTTP endpoint that writes a feedback attachment as raw bytes.
#endregion

#region Design
// Hand-written because the generated endpoint JSON-serializes TResponse, and img src cannot
// read that. HandleAsync replaces the base writer. Problems stay application/problem+json
// with the problem's own status, matching BaseFastEndpoint. Images are inline; other
// allow-listed types are attachments. nosniff and a private cache header go on every success.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Architecture.Features.Feedback.Application;
using TimeWarp.Foundation.Features;

/// <summary>GET api/Feedback/attachments/{id}</summary>
public sealed class DownloadFeedbackAttachmentEndpoint
  : BaseFastEndpoint<DownloadFeedbackAttachment.Query, DownloadFeedbackAttachment.Response>
{
  /// <inheritdoc />
  public override void Configure()
  {
    Get(DownloadFeedbackAttachment.Query.RouteTemplate);
    AuthSchemes(AuthenticationSchemeNames.IdentitySession, AuthenticationSchemeNames.MockIdentitySession);
    Policies(PermissionIds.FeedbackFileSelf);
  }

  /// <inheritdoc />
  public override async Task HandleAsync(
    DownloadFeedbackAttachment.Query request,
    CancellationToken cancellationToken)
  {
    ISender sender = HttpContext.RequestServices.GetRequiredService<ISender>();
    OneOf<DownloadFeedbackAttachment.Response, SharedProblemDetails> oneOf =
      await sender.Send(request, cancellationToken).ConfigureAwait(false);

    await oneOf.Match<Task>(
      async success =>
      {
        HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        HttpContext.Response.ContentType = FeedbackAttachmentHttp.SafeContentType(success.ContentType);
        HttpContext.Response.Headers.ContentDisposition =
          FeedbackAttachmentHttp.ContentDisposition(success.FileName, success.ContentType);
        HttpContext.Response.Headers.XContentTypeOptions = "nosniff";
        HttpContext.Response.Headers.CacheControl = "private, no-store";
        if (success.Content.CanSeek)
        {
          HttpContext.Response.ContentLength = success.Content.Length;
        }

        await using Stream content = success.Content;
        await content.CopyToAsync(HttpContext.Response.Body, cancellationToken).ConfigureAwait(false);
      },
      async problem =>
      {
        HttpContext.Response.ContentType = "application/problem+json; charset=utf-8";
        HttpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status400BadRequest;
        await HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken).ConfigureAwait(false);
      });
  }
}
