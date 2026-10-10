#region Purpose
// HTTP endpoint for a raw-body feedback attachment upload.
#endregion

#region Design
// Hand-written BaseFastEndpoint, not [ApiEndpoint]: the generator's success path is JSON
// and its binder cannot take the request stream. TWA0006 still sees this subclass.
// Auth matches the contract: feedback.file.self on both human session schemes.
// MaxRequestBodySize is the same cap the handler enforces.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

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
    MaxRequestBodySize(FeedbackAttachmentRules.MaxBytes);
  }
}
