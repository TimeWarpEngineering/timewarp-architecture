#region Purpose
// Port for sending one email. Feedback tests fake it; the host registers a development sender.
#endregion

#region Design
// No credentials and no provider SDK. DevelopmentEmailSender logs and, when Mail:PickupDirectory
// is set, writes a file. Replacing the registration is the seam a real provider would use.
#endregion

namespace TimeWarp.Architecture.Mail;

public interface IEmailSender
{
  Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
