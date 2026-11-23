#region Purpose
// Bindable xAI key-policy mode shared by UpdateXaiByokMode.Command and GetSiteSettings.Response.
#endregion

#region Design
// One field, so the AI page copies the stored mode with generated From instead of assigning
// XaiByokMode by hand. Version stays off this interface: it is the concurrency token, and
// UpdateXaiByokMode.Command copies it in CopyBeyondDetails from GetSiteSettings.Response.
// The mode must be a defined XaiByokMode. The host XAI:ApiKey still wins at resolve time.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using TimeWarp.Identity;

public interface IXaiByokModeDetails
{
  public XaiByokMode XaiByokMode { get; set; }
}

public sealed class XaiByokModeDetailsValidator : AbstractValidator<IXaiByokModeDetails>
{
  public XaiByokModeDetailsValidator()
  {
    RuleFor(details => details.XaiByokMode).IsInEnum();
  }
}
