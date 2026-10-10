#region Purpose
// Returns one feedback item to its owner.
#endregion

#region Design
// A missing id and an id owned by another principal are both 404. The response kind is mapped
// back by name onto the contract enum. Attachments are the rows linked to this item.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using TimeWarp.Architecture.Features.Feedback.Domain;
using static TimeWarp.Architecture.Features.Feedback.GetFeedback;
using ContractKind = TimeWarp.Architecture.Features.Feedback.FeedbackKind;
using DownloadFeedbackAttachmentContract = TimeWarp.Architecture.Features.Feedback.DownloadFeedbackAttachment;

public sealed class GetFeedback
{
  public sealed class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IFeedbackStore FeedbackStore;
    private readonly IFeedbackAttachmentStore AttachmentStore;

    public Handler(
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IFeedbackStore feedbackStore,
      IFeedbackAttachmentStore attachmentStore)
    {
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      FeedbackStore = feedbackStore;
      AttachmentStore = attachmentStore;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Query request,
      CancellationToken cancellationToken)
    {
      PrincipalId? principalId = await CurrentPrincipalAccessor
        .GetCurrentPrincipalIdAsync(cancellationToken)
        .ConfigureAwait(false);
      if (principalId is null)
      {
        return FeedbackProblems.Unauthenticated();
      }

      FeedbackItem? item = await FeedbackStore
        .FindAsync(FeedbackItemId.From(request.FeedbackItemId), cancellationToken)
        .ConfigureAwait(false);
      if (item is null || item.OwnerPrincipalId != principalId.Value.Value)
      {
        return FeedbackProblems.NotFound();
      }

      if (!Enum.TryParse(item.Kind.ToString(), out ContractKind kind) || !Enum.IsDefined(kind))
      {
        throw new InvalidOperationException($"Stored feedback kind '{item.Kind}' has no contract member.");
      }

      IReadOnlyList<FeedbackAttachment> stored = await AttachmentStore
        .ListByItemAsync(item.Id, cancellationToken)
        .ConfigureAwait(false);
      var attachments = new List<Attachment>(stored.Count);
      foreach (FeedbackAttachment attachment in stored)
      {
        attachments.Add(new Attachment(
          attachment.Id.Value,
          attachment.FileName,
          attachment.ContentType,
          attachment.Size,
          DownloadFeedbackAttachmentContract.DownloadPath(attachment.Id.Value)));
      }

      return new Response(
        item.Id.Value,
        FeedbackPermalink.For(item.Id.Value),
        kind,
        item.Title,
        item.Body,
        item.FiledAt,
        attachments);
    }
  }
}
