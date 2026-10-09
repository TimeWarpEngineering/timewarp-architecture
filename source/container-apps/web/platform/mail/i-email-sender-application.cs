#region Purpose
// Port for sending one email. Feedback tests fake it; the host registers the sender Mail:Sender selects.
#endregion

#region Design
// No credentials and no provider SDK. DevelopmentEmailSender (Mail:Sender "Development") logs and,
// when Mail:PickupDirectory is set, writes a file; otherwise UnconfiguredEmailSender throws.
// Replacing the registration is the seam a real provider would use. A send may throw; callers that
// have already committed work treat mail as best-effort.
#endregion

namespace TimeWarp.Architecture.Mail;

public interface IEmailSender
{
  Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
