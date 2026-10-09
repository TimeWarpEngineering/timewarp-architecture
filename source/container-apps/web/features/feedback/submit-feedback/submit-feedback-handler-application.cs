#region Purpose
// Files one feedback item for the authenticated principal and optionally emails a copy.
#endregion

#region Design
// Owner comes from ICurrentPrincipalAccessor, never from the client. Kind maps by enum name onto
// the domain enum. The store write happens before mail. Mail is best-effort after the commit: the
// email lookup and send run with CancellationToken.None (the row already exists, so a cancelled
// request must not lose its receipt), and any failure other than OperationCanceledException is
// logged as a warning and reported as EmailCopySent=false. The filer always gets the id and
// permalink once the row is stored, so a retry never files a duplicate. EmailCopySent is true only
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

    public Handler(
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IFeedbackStore feedbackStore,
      IProfileEmailLookup profileEmailLookup,
      IEmailSender emailSender,
      IAppBaseUrlAccessor appBaseUrlAccessor,
      ILogger<Handler> logger)
    {
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      FeedbackStore = feedbackStore;
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

      if (!Enum.TryParse(request.Kind.ToString(), out DomainKind kind) || !Enum.IsDefined(kind))
      {
        return new SharedProblemDetails
        {
          Title = "Invalid kind",
          Status = 400,
          Detail = "Kind must be a defined feedback kind."
        };
      }

      var item = FeedbackItem.File(
        principalId.Value.Value,
        kind,
        request.Title,
        request.Body,
        DateTimeOffset.UtcNow);
      await FeedbackStore.AddAsync(item, cancellationToken).ConfigureAwait(false);

      string permalink = FeedbackPermalink.For(item.Id.Value);
      bool emailCopySent = request.EmailCopy
        && await TrySendCopyAsync(principalId.Value.Value, item, kind, permalink).ConfigureAwait(false);

      return new Response(
        item.Id.Value,
        permalink,
        request.Kind,
        item.Title,
        item.Body,
        emailCopySent);
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
