#region Purpose
// Shared bindable shape for GetSiteSettings / UpdateSiteSettings (Entra policy + passkey prompt).
#endregion

#region Design
// I*Details so the Settings Authentication section binds one shape. Tenant ids are GUID strings
// for the editor (textarea / list). Version is not on this interface — it is the concurrency
// token on Command/Response, not a form field the user types. PasskeyPromptMode uses the
// TimeWarp.Identity enum (JsonStringEnumConverter). Validator: tenants must parse as non-empty
// GUIDs; PasskeyPromptMode must be defined.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using TimeWarp.Identity;

public interface ISiteSettingsDetails
{
  bool EntraSignInEnabled { get; set; }
  bool EntraAllowBootstrap { get; set; }
  List<string> EntraTrustedTenants { get; set; }
  PasskeyPromptMode PasskeyPromptMode { get; set; }
}

public sealed class SiteSettingsDetailsValidator : AbstractValidator<ISiteSettingsDetails>
{
  public SiteSettingsDetailsValidator()
  {
    RuleForEach(details => details.EntraTrustedTenants)
      .Must(BeNonEmptyGuid)
      .WithMessage("Each trusted tenant id must be a GUID.");

    RuleFor(details => details.PasskeyPromptMode)
      .IsInEnum();
  }

  private static bool BeNonEmptyGuid(string? value) =>
    Guid.TryParse(value, out Guid tenantId) && tenantId != Guid.Empty;
}
