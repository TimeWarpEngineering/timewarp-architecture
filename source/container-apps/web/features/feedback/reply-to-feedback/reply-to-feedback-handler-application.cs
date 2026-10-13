#region Purpose
// Adds an administrator's private reply to a feedback item and emails it to the filer.
#endregion

#region Design
// The author is the authenticated administrator. Store write before mail; a concurrent writer
// is 409 Changed and nothing is sent. Every admin reply is emailed when the filer's profile has
// an address (task 314 decision 3); FilerEmailed reports the outcome.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features.Feedback.Domain;
using TimeWarp.Architecture.Mail;
using static TimeWarp.Architecture.Features.Feedback.ReplyToFeedback;

public sealed class ReplyToFeedback
{
  public sealed class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IFeedbackStore FeedbackStore;
    private readonly IFeedbackAttachmentStore AttachmentStore;
    private readonly IProfileEmailLookup ProfileEmailLookup;
    private readonly FeedbackOwnerNames OwnerNames;
    private readonly IEmailSender EmailSender;
    private readonly IAppBaseUrlAccessor AppBaseUrlAccessor;
    private readonly ILogger<Handler> Logger;

    public Handler(
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IFeedbackStore feedbackStore,
      IFeedbackAttachmentStore attachmentStore,
      IProfileEmailLookup profileEmailLookup,
      FeedbackOwnerNames ownerNames,
      IEmailSender emailSender,
      IAppBaseUrlAccessor appBaseUrlAccessor,
      ILogger<Handler> logger)
    {
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      FeedbackStore = feedbackStore;
      AttachmentStore = attachmentStore;
      ProfileEmailLookup = profileEmailLookup;
      OwnerNames = ownerNames;
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

      if (!FeedbackProblems.IsValidMessage(request.Body))
      {
        return FeedbackProblems.InvalidMessage();
      }

      FeedbackItem? item = await FeedbackStore
        .FindAsync(FeedbackItemId.From(request.FeedbackItemId), cancellationToken)
        .ConfigureAwait(false);
      if (item is null)
      {
        return FeedbackProblems.NotFound();
      }

      FeedbackActivity reply = item.AddAdminReply(principalId.Value.Value, request.Body, DateTimeOffset.UtcNow);
      if (!await FeedbackStore.UpdateAsync(item, cancellationToken).ConfigureAwait(false))
      {
        return FeedbackProblems.Changed();
      }

      bool filerEmailed = await FeedbackFilerMail
        .SendReplyAsync(item, reply.Body!, ProfileEmailLookup, EmailSender, AppBaseUrlAccessor, Logger)
        .ConfigureAwait(false);

      FeedbackAdminView view = await FeedbackContractMap
        .AdminViewAsync(item, AttachmentStore, ProfileEmailLookup, OwnerNames, cancellationToken)
        .ConfigureAwait(false);
      return new Response(view, filerEmailed);
    }
  }
}
