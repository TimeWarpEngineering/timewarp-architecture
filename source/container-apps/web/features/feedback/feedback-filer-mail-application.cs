#region Purpose
// Best-effort email to a feedback filer: admin replies and notifying status changes.
#endregion

#region Design
// Task 314 decisions 2 and 3 reuse the 273 receipt path: IProfileEmailLookup for the address
// (no profile row is created) and IEmailSender for the send. Mail runs after the store write
// with CancellationToken.None, like the receipt, so a cancelled request cannot lose a mail for
// a change that already committed. Any failure other than cancellation is logged as a warning
// and reported as false; the write is never undone because mail failed. The permalink becomes
// absolute when IAppBaseUrlAccessor knows the public origin. Internal notes never reach here.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features.Feedback.Domain;
using TimeWarp.Architecture.Mail;

internal static class FeedbackFilerMail
{
  private static readonly Action<ILogger, Guid, Exception?> LogFilerMailFailed =
    LoggerMessage.Define<Guid>
    (
      LogLevel.Warning,
      new EventId(3, nameof(LogFilerMailFailed)),
      "Feedback {FeedbackItemId} was updated but the email to its filer failed"
    );

  public static Task<bool> SendReplyAsync(
    FeedbackItem item,
    string reply,
    IProfileEmailLookup profileEmailLookup,
    IEmailSender emailSender,
    IAppBaseUrlAccessor appBaseUrlAccessor,
    ILogger logger) =>
    TrySendAsync(
      item,
      $"Reply to your feedback {item.Id.Value:D}",
      $"""
      An administrator replied to your feedback "{item.Title}".

      {reply}

      Open it: {Link(item, appBaseUrlAccessor)}
      """,
      profileEmailLookup,
      emailSender,
      logger);

  public static Task<bool> SendStatusChangeAsync(
    FeedbackItem item,
    FeedbackStatus status,
    IProfileEmailLookup profileEmailLookup,
    IEmailSender emailSender,
    IAppBaseUrlAccessor appBaseUrlAccessor,
    ILogger logger) =>
    TrySendAsync(
      item,
      $"Your feedback {item.Id.Value:D} is {status}",
      $"""
      Your feedback "{item.Title}" is now {status}.

      Open it: {Link(item, appBaseUrlAccessor)}
      """,
      profileEmailLookup,
      emailSender,
      logger);

  private static async Task<bool> TrySendAsync(
    FeedbackItem item,
    string subject,
    string body,
    IProfileEmailLookup profileEmailLookup,
    IEmailSender emailSender,
    ILogger logger)
  {
    try
    {
      string? email = await profileEmailLookup
        .FindEmailAsync(item.OwnerPrincipalId, CancellationToken.None)
        .ConfigureAwait(false);
      if (string.IsNullOrWhiteSpace(email))
      {
        return false;
      }

      await emailSender.SendAsync(new EmailMessage(email, subject, body), CancellationToken.None)
        .ConfigureAwait(false);
      return true;
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
      LogFilerMailFailed(logger, item.Id.Value, exception);
      return false;
    }
  }

  private static string Link(FeedbackItem item, IAppBaseUrlAccessor appBaseUrlAccessor)
  {
    string permalink = FeedbackPermalink.For(item.Id.Value);
    Uri? baseUrl = appBaseUrlAccessor.GetBaseUrl();
    return baseUrl is null ? permalink : baseUrl.AbsoluteUri.TrimEnd('/') + permalink;
  }
}
