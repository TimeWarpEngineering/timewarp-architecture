#region Purpose
// Shared bindable shape for GetSiteSettings / UpdateSiteSettings (Entra policy, passkey prompt,
// assistant name).
#endregion

#region Design
// I*Details so the Settings Authentication section binds one shape. Version is not on this
// interface — it is the concurrency token on Command/Response, not a form field the user types.
// PasskeyPromptMode uses the TimeWarp.Identity enum (JsonStringEnumConverter). Validator:
// PasskeyPromptMode must be defined. Task 227 dropped trusted-tenant GUIDs from this shape;
// trust is Authentication:Entra:TenantId. Task 361 added AssistantName: required, and trimmed it
// must be 1-32 plain-text characters (AssistantNameRules, the same rules SiteSettings enforces).
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using TimeWarp.Identity;

public interface ISiteSettingsDetails
{
  bool EntraSignInEnabled { get; set; }
  bool EntraAllowBootstrap { get; set; }
  PasskeyPromptMode PasskeyPromptMode { get; set; }
  string AssistantName { get; set; }
}

public sealed class SiteSettingsDetailsValidator : AbstractValidator<ISiteSettingsDetails>
{
  public SiteSettingsDetailsValidator()
  {
    RuleFor(details => details.PasskeyPromptMode)
      .IsInEnum();
    RuleFor(details => details.AssistantName)
      .NotEmpty()
      .WithMessage("Assistant name is required.");
    RuleFor(details => details.AssistantName)
      .Must(static name => name is null || name.Trim().Length <= AssistantNameRules.MaxLength)
      .WithMessage($"Assistant name must be {AssistantNameRules.MaxLength} characters or fewer.");
    RuleFor(details => details.AssistantName)
      .Must(static name => name is null || AssistantNameRules.IsPlainText(name))
      .WithMessage("Assistant name must be plain text, without markup or control characters.");
  }
}
