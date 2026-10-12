#region Purpose
// Moves a feedback item to another status, records it in the thread, and emails the filer when
// the new status is one the filer is told about.
#endregion

#region Design
// The author is the authenticated administrator (ICurrentPrincipalAccessor), never the client.
// New and undefined statuses are 400; the current status is 409 (the aggregate refuses both,
// and the handler answers first so the client gets a problem, not an exception). Store write
// before mail. A concurrent writer is 409 Changed. Mail only when
// FeedbackStatusRules.NotifiesFiler(status) — UnderReview / InProgress churn stays in-app.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features.Feedback.Domain;
using TimeWarp.Architecture.Mail;
using static TimeWarp.Architecture.Features.Feedback.ChangeFeedbackStatus;
using DomainStatus = TimeWarp.Architecture.Features.Feedback.Domain.FeedbackStatus;

public sealed class ChangeFeedbackStatus
{
  public sealed class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IFeedbackStore FeedbackStore;
    private readonly IFeedbackAttachmentStore AttachmentStore;
    private readonly IProfileEmailLookup ProfileEmailLookup;
    private readonly IEmailSender EmailSender;
    private readonly IAppBaseUrlAccessor AppBaseUrlAccessor;
    private readonly ILogger<Handler> Logger;

    public Handler(
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IFeedbackStore feedbackStore,
      IFeedbackAttachmentStore attachmentStore,
      IProfileEmailLookup profileEmailLookup,
      IEmailSender emailSender,
      IAppBaseUrlAccessor appBaseUrlAccessor,
      ILogger<Handler> logger)
    {
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      FeedbackStore = feedbackStore;
      AttachmentStore = attachmentStore;
      ProfileEmailLookup = profileEmailLookup;
      EmailSender = emailSender;
      AppBaseUrlAccessor = appBaseUrlAccessor;
      Logger = logger;
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

      if (FeedbackContractMap.ToDomain(request.Status) is not { } status || status == DomainStatus.New)
      {
        return FeedbackProblems.InvalidStatus();
      }

      FeedbackItem? item = await FeedbackStore
        .FindAsync(FeedbackItemId.From(request.FeedbackItemId), cancellationToken)
        .ConfigureAwait(false);
      if (item is null)
      {
        return FeedbackProblems.NotFound();
      }

      if (!item.CanChangeStatusTo(status))
      {
        return FeedbackProblems.StatusUnchanged(status.ToString());
      }

      item.ChangeStatus(status, principalId.Value.Value, DateTimeOffset.UtcNow);
      if (!await FeedbackStore.UpdateAsync(item, cancellationToken).ConfigureAwait(false))
      {
        return FeedbackProblems.Changed();
      }

      bool filerEmailed = FeedbackStatusRules.NotifiesFiler(request.Status)
        && await FeedbackFilerMail
          .SendStatusChangeAsync(item, status, ProfileEmailLookup, EmailSender, AppBaseUrlAccessor, Logger)
          .ConfigureAwait(false);

      FeedbackAdminView view = await FeedbackContractMap
        .AdminViewAsync(item, AttachmentStore, ProfileEmailLookup, cancellationToken)
        .ConfigureAwait(false);
      return new Response(view, filerEmailed);
    }
  }
}
