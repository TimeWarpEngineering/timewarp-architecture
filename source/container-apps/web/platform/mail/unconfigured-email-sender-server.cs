#region Purpose
// IEmailSender used when no mail provider is configured: every send throws.
#endregion

#region Design
// Registered by MailModule unless Mail:Sender is "Development". Throwing (instead of a silent
// no-op) keeps EmailCopySent honest: the feedback handler catches the failure, logs a warning, and
// reports EmailCopySent=false while still returning the filing's id and permalink.
#endregion

namespace TimeWarp.Architecture.Mail;

public sealed class UnconfiguredEmailSender : IEmailSender
{
  public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default) =>
    throw new InvalidOperationException("No mail provider is configured");
}
