#region Purpose
// One outbound mail message: recipient, subject, and body.
#endregion

#region Design
// No attachments and no from-address. The development sender supplies a fixed from-line in the
// pickup file. A real provider would own the from-address outside this type.
#endregion

namespace TimeWarp.Architecture.Mail;

public sealed class EmailMessage
{
  public EmailMessage(string to, string subject, string body)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(to);
    ArgumentException.ThrowIfNullOrWhiteSpace(subject);
    ArgumentException.ThrowIfNullOrWhiteSpace(body);
    To = to.Trim();
    Subject = subject.Trim();
    Body = body;
  }

  public string To { get; }
  public string Subject { get; }
  public string Body { get; }
}
