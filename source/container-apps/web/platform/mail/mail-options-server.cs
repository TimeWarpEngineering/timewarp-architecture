#region Purpose
// Configuration for mail: which sender to use, the development pickup directory, and the public origin.
#endregion

#region Design
// Section "Mail". Sender "Development" opts into the development sender; anything else means no
// provider is configured. PickupDirectory empty or absent means log only. PublicBaseUrl, when set,
// is the absolute origin used for emailed permalinks (see HttpAppBaseUrlAccessor); it is a Uri
// (CA1056) and the configuration binder parses it from the string value. No credentials live here.
#endregion

namespace TimeWarp.Architecture.Mail;

public sealed class MailOptions
{
  public const string SectionName = "Mail";
  public const string DevelopmentSender = "Development";

  public string? Sender { get; set; }
  public string? PickupDirectory { get; set; }
  public Uri? PublicBaseUrl { get; set; }
}
