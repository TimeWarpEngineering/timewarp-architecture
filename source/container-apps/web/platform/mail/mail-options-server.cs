#region Purpose
// Configuration for the development mail sender.
#endregion

#region Design
// Section "Mail". PickupDirectory empty or absent means log only. No credentials live here.
#endregion

namespace TimeWarp.Architecture.Mail;

public sealed class MailOptions
{
  public const string SectionName = "Mail";

  public string? PickupDirectory { get; set; }
}
