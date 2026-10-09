#region Purpose
// Development IEmailSender: log every message and optionally drop it in a pickup directory.
#endregion

#region Design
// Registered only when Mail:Sender is "Development" (MailModule). No network and no secrets.
// Logging: To and Subject at Information, the body only at Debug so feedback text stays out of
// default telemetry. A non-empty Mail:PickupDirectory is created if needed and receives one .eml
// file per send with a fixed "From: no-reply@localhost" line. IO failures propagate so a
// misconfigured pickup is visible; the feedback handler turns them into EmailCopySent=false.
// A production provider replaces this registration; it needs a host, a credential, and a from-address.
#endregion

namespace TimeWarp.Architecture.Mail;

using Microsoft.Extensions.Logging;

public sealed class DevelopmentEmailSender : IEmailSender
{
  private const string FromAddress = "no-reply@localhost";

  private static readonly Action<ILogger, string, string, Exception?> LogMail =
    LoggerMessage.Define<string, string>
    (
      LogLevel.Information,
      new EventId(1, nameof(LogMail)),
      "Development mail to {To} subject {Subject}"
    );

  private static readonly Action<ILogger, string, Exception?> LogMailBody =
    LoggerMessage.Define<string>
    (
      LogLevel.Debug,
      new EventId(2, nameof(LogMailBody)),
      "Development mail body {Body}"
    );

  private readonly ILogger<DevelopmentEmailSender> Logger;
  private readonly MailOptions Options;

  public DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger, IOptions<MailOptions> options)
  {
    Logger = logger;
    Options = options.Value;
  }

  public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(message);
    cancellationToken.ThrowIfCancellationRequested();
    LogMail(Logger, message.To, message.Subject, null);
    LogMailBody(Logger, message.Body, null);

    if (string.IsNullOrWhiteSpace(Options.PickupDirectory))
    {
      return;
    }

    Directory.CreateDirectory(Options.PickupDirectory);
    string path = Path.Combine(
      Options.PickupDirectory,
      $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.eml");
    string content =
      $"""
      From: {FromAddress}
      To: {message.To}
      Subject: {message.Subject}

      {message.Body}
      """;
    await File.WriteAllTextAsync(path, content, cancellationToken).ConfigureAwait(false);
  }
}
