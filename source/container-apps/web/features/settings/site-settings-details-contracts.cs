#region Purpose
// Shared bindable shape for GetSiteSettings / UpdateSiteSettings (Entra policy + passkey prompt).
#endregion

#region Design
// I*Details so the Settings Authentication section binds one shape. Version is not on this
// interface — it is the concurrency token on Command/Response, not a form field the user types.
// PasskeyPromptMode uses the TimeWarp.Identity enum (JsonStringEnumConverter). Validator:
// PasskeyPromptMode must be defined. Task 227 dropped trusted-tenant GUIDs from this shape;
// trust is Authentication:Entra:TenantId.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using TimeWarp.Identity;

public interface ISiteSettingsDetails
{
  bool EntraSignInEnabled { get; set; }
  bool EntraAllowBootstrap { get; set; }
  PasskeyPromptMode PasskeyPromptMode { get; set; }
}

public sealed class SiteSettingsDetailsValidator : AbstractValidator<ISiteSettingsDetails>
{
  public SiteSettingsDetailsValidator()
  {
    RuleFor(details => details.PasskeyPromptMode)
      .IsInEnum();
  }
}
