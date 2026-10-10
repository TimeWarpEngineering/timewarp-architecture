#region Purpose
// Files one feedback item for the authenticated principal and optionally emails a copy.
#endregion

#region Design
// Owner comes from ICurrentPrincipalAccessor, never from the client. Kind maps by enum name onto
// the domain enum. The store write happens before mail. Mail is best-effort after the commit: the
// email lookup and send run with CancellationToken.None (the row already exists, so a cancelled
// request must not lose its receipt), and any failure other than OperationCanceledException is
// logged as a warning and reported as EmailCopySent=false. The filer always gets the id and
// permalink once the row is stored, so a retry never files a duplicate. Attachment ids are
// checked before File: each must be this principal's unlinked upload, or nothing is filed.
// The pre-check gives the common case a clean 400 without writing. The link itself is
// TryLinkAsync, a conditional write, because a remove or a second submit can run between the
// check and the link. The item is inserted first (the attachment row has a foreign key to it);
// if any TryLink fails or a store throws, every link to the item is undone by item id (so a
// link that committed and then threw is undone too) and the item is removed, so a successful
// rollback never leaves an item with only some of its files. If a rollback step itself throws,
// it is logged at Error and the item can stay filed, possibly partly linked. Once the item is stored, linking and
// rollback run with CancellationToken.None so a cancelled request cannot stop halfway. The
// two rollback steps run independently and log their own failures; the original exception
// is rethrown. Unlinking before the delete matters: the attachment FK cascades, so a delete
// with links still in place would drop those rows and orphan their blobs. If the unlink step
// itself fails, that cascade can still happen; it is logged. A full database transaction
// would need a unit-of-work port spanning both stores; compensation keeps the store ports
// independent. A 400 lists the unavailable ids (pre-check and lost race) so the client can
// drop them from its draft. EmailCopySent is true only
// when the profile already has an email, the filer opted in, and the send completed. No profile
// row is created. The permalink in the response is the relative path; the message body uses an
// absolute URL when IAppBaseUrlAccessor returns a public origin.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features.Feedback.Domain;
using TimeWarp.Architecture.Mail;
using static TimeWarp.Architecture.Features.Feedback.SubmitFeedback;
using DomainKind = TimeWarp.Architecture.Features.Feedback.Domain.FeedbackKind;

public sealed class SubmitFeedback
{
  public sealed class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private static readonly Action<ILogger, Guid, string, Exception?> LogRollbackFailed =
      LoggerMessage.Define<Guid, string>
      (
        LogLevel.Error,
        new EventId(2, nameof(LogRollbackFailed)),
        "Feedback {FeedbackItemId} could not be rolled back: {RollbackStep} failed"
      );

    private static readonly Action<ILogger, Guid, Exception?> LogEmailCopyFailed =
      LoggerMessage.Define<Guid>
      (
        LogLevel.Warning,
        new EventId(1, nameof(LogEmailCopyFailed)),
        "Feedback {FeedbackItemId} was filed but its email copy failed"
      );

    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IFeedbackStore FeedbackStore;
    private readonly IProfileEmailLookup ProfileEmailLookup;
    private readonly IEmailSender EmailSender;
    private readonly IAppBaseUrlAccessor AppBaseUrlAccessor;
    private readonly ILogger<Handler> Logger;
    private readonly IFeedbackAttachmentStore AttachmentStore;

    public Handler(
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IFeedbackStore feedbackStore,
      IProfileEmailLookup profileEmailLookup,
      IEmailSender emailSender,
      IAppBaseUrlAccessor appBaseUrlAccessor,
      ILogger<Handler> logger,
      IFeedbackAttachmentStore attachmentStore)
    {
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      FeedbackStore = feedbackStore;
      ProfileEmailLookup = profileEmailLookup;
      EmailSender = emailSender;
      AppBaseUrlAccessor = appBaseUrlAccessor;
      Logger = logger;
      AttachmentStore = attachmentStore;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Command request,
      CancellationToken cancellationToken)
    {
      PrincipalId? principalId = await CurrentPrincipalAccessor
        .GetCurrentPrincipalIdAsync(cancellationToken)
        .ConfigureAwait(false);
      if (principalId is null)
      {
        return FeedbackProblems.Unauthenticated();
      }

      if (!Enum.TryParse(request.Kind.ToString(), out DomainKind kind) || !Enum.IsDefined(kind))
      {
        return new SharedProblemDetails
        {
          Title = "Invalid kind",
          Status = 400,
          Detail = "Kind must be a defined feedback kind."
        };
      }

      List<Guid> requested = request.AttachmentIds ?? [];
      if (requested.Count > FeedbackAttachment.MaxPerItem || requested.Distinct().Count() != requested.Count)
      {
        return FeedbackProblems.AttachmentUnavailable();
      }

      List<FeedbackAttachment> attachments = [];
      List<Guid> unavailable = [];
      foreach (Guid attachmentId in requested)
      {
        FeedbackAttachment? attachment = await AttachmentStore
          .FindAsync(FeedbackAttachmentId.From(attachmentId), cancellationToken)
          .ConfigureAwait(false);
        if (attachment is null
          || attachment.OwnerPrincipalId != principalId.Value.Value
          || attachment.FeedbackItemId is not null)
        {
          unavailable.Add(attachmentId);
          continue;
        }

        attachments.Add(attachment);
      }

      if (unavailable.Count > 0)
      {
        return FeedbackProblems.AttachmentUnavailable(unavailable);
      }

      var item = FeedbackItem.File(
        principalId.Value.Value,
        kind,
        request.Title,
        request.Body,
        DateTimeOffset.UtcNow);
      await FeedbackStore.AddAsync(item, cancellationToken).ConfigureAwait(false);
      FeedbackAttachmentId? lost = await LinkAllOrRollBackAsync(principalId.Value.Value, item.Id, attachments)
        .ConfigureAwait(false);
      if (lost is { } lostId)
      {
        return FeedbackProblems.AttachmentUnavailable([lostId.Value]);
      }

      string permalink = FeedbackPermalink.For(item.Id.Value);
      bool emailCopySent = request.EmailCopy
        && await TrySendCopyAsync(principalId.Value.Value, item, kind, permalink).ConfigureAwait(false);

      return new Response(
        item.Id.Value,
        permalink,
        request.Kind,
        item.Title,
        item.Body,
        emailCopySent,
        attachments.ConvertAll(attachment => attachment.Id.Value));
    }

    /// <summary>Links every attachment, or rolls the filing back and returns the id that was lost.</summary>
    private async Task<FeedbackAttachmentId?> LinkAllOrRollBackAsync(
      Guid ownerId,
      FeedbackItemId itemId,
      List<FeedbackAttachment> attachments)
    {
      try
      {
        foreach (FeedbackAttachment attachment in attachments)
        {
          bool ok = await AttachmentStore
            .TryLinkAsync(attachment.Id, ownerId, itemId, CancellationToken.None)
            .ConfigureAwait(false);
          if (!ok)
          {
            await RollBackAsync(itemId).ConfigureAwait(false);
            return attachment.Id;
          }
        }
      }
      catch
      {
        await RollBackAsync(itemId).ConfigureAwait(false);
        throw;
      }

      return null;
    }

    private async Task RollBackAsync(FeedbackItemId itemId)
    {
      try
      {
        await AttachmentStore.UnlinkAllAsync(itemId, CancellationToken.None).ConfigureAwait(false);
      }
      catch (Exception exception)
      {
        LogRollbackFailed(Logger, itemId.Value, "unlink", exception);
      }

      try
      {
        await FeedbackStore.RemoveAsync(itemId, CancellationToken.None).ConfigureAwait(false);
      }
      catch (Exception exception)
      {
        LogRollbackFailed(Logger, itemId.Value, "remove item", exception);
      }
    }

    private async Task<bool> TrySendCopyAsync(Guid ownerId, FeedbackItem item, DomainKind kind, string permalink)
    {
      try
      {
        string? email = await ProfileEmailLookup
          .FindEmailAsync(ownerId, CancellationToken.None)
          .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(email))
        {
          return false;
        }

        await EmailSender.SendAsync(
          new EmailMessage(email, $"Feedback {item.Id.Value:D}", BuildBody(item, kind, permalink)),
          CancellationToken.None).ConfigureAwait(false);
        return true;
      }
      catch (Exception exception) when (exception is not OperationCanceledException)
      {
        LogEmailCopyFailed(Logger, item.Id.Value, exception);
        return false;
      }
    }

    private string BuildBody(FeedbackItem item, DomainKind kind, string permalink)
    {
      Uri? baseUrl = AppBaseUrlAccessor.GetBaseUrl();
      string link = baseUrl is null
        ? permalink
        : baseUrl.AbsoluteUri.TrimEnd('/') + permalink;
      return
        $"""
        Id: {item.Id.Value:D}
        Kind: {kind}
        Title: {item.Title}
        Permalink: {link}

        {item.Body}
        """;
    }
  }
}
