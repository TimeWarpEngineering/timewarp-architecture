#region Purpose
// Development IEmailSender: log every message and optionally drop it in a pickup directory.
#endregion

#region Design
// No network and no secrets. A non-empty Mail:PickupDirectory is created if needed and receives
// one .eml file per send. IO failures propagate so a misconfigured pickup is visible.
// A production provider replaces this registration; it needs a host, a credential, and a from-address.
#endregion

namespace TimeWarp.Architecture.Mail;

using Microsoft.Extensions.Logging;

public sealed class DevelopmentEmailSender : IEmailSender
{
  private static readonly Action<ILogger, string, string, string, Exception?> LogMail =
    LoggerMessage.Define<string, string, string>
    (
      LogLevel.Information,
      new EventId(1, nameof(LogMail)),
      "Development mail to {To} subject {Subject} body {Body}"
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
    LogMail(Logger, message.To, message.Subject, message.Body, null);

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
      To: {message.To}
      Subject: {message.Subject}

      {message.Body}
      """;
    await File.WriteAllTextAsync(path, content, cancellationToken).ConfigureAwait(false);
  }
}
