#region Purpose
// One outbound mail message: recipient, subject, and body.
#endregion

#region Design
// No attachments and no from-address. The development sender writes a fixed
// "From: no-reply@localhost" line in its pickup file. A real provider would own the from-address
// outside this type. To and Subject become header lines, so the constructor rejects '\r' and '\n'
// in either (ArgumentException) to stop header injection from an unverified profile email. The
// body may span lines.
#endregion

namespace TimeWarp.Architecture.Mail;

public sealed class EmailMessage
{
  public EmailMessage(string to, string subject, string body)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(to);
    ArgumentException.ThrowIfNullOrWhiteSpace(subject);
    ArgumentException.ThrowIfNullOrWhiteSpace(body);
    RejectLineBreaks(to, nameof(to));
    RejectLineBreaks(subject, nameof(subject));
    To = to.Trim();
    Subject = subject.Trim();
    Body = body;
  }

  public string To { get; }
  public string Subject { get; }
  public string Body { get; }

  private static void RejectLineBreaks(string value, string paramName)
  {
    if (value.AsSpan().IndexOfAny('\r', '\n') >= 0)
    {
      throw new ArgumentException("A mail header value must not contain a line break.", paramName);
    }
  }
}
