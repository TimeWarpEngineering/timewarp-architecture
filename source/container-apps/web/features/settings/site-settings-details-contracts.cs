#region Purpose
// Shared bindable shape for GetSiteSettings / UpdateSiteSettings (Entra policy, passkey prompt,
// assistant name).
#endregion

#region Design
// I*Details so the Settings Authentication section binds one shape. Version is not on this
// interface — it is the concurrency token on Command/Response, not a form field the user types.
// PasskeyPromptMode and EntraTenantAcceptanceMode use TimeWarp.Identity enums
// (JsonStringEnumConverter). Validator: both enums must be defined. Accepted tenant ids are a
// list of non-empty GUIDs, at most EntraTenantIds.MaxAcceptedCount; an empty list is valid and
// means Restricted accepts nobody. Task 306: this shape is the trust policy. Task 361 added
// AssistantName: required, and trimmed it must be 1-32 plain-text characters. Beyond NotEmpty
// (the required message) the validator calls AssistantNameRules.IsValid, the same check
// SiteSettings.RenameAssistant runs, so they cannot drift.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using TimeWarp.Identity;

public interface ISiteSettingsDetails
{
  bool EntraSignInEnabled { get; set; }
  bool EntraAllowBootstrap { get; set; }
  PasskeyPromptMode PasskeyPromptMode { get; set; }
  string AssistantName { get; set; }
  EntraTenantAcceptanceMode EntraTenantAcceptanceMode { get; set; }
  List<Guid> EntraAcceptedTenantIds { get; set; }
}

public sealed class SiteSettingsDetailsValidator : AbstractValidator<ISiteSettingsDetails>
{
  public SiteSettingsDetailsValidator()
  {
    RuleFor(details => details.PasskeyPromptMode)
      .IsInEnum();
    RuleFor(details => details.EntraTenantAcceptanceMode)
      .IsInEnum();
    RuleFor(details => details.EntraAcceptedTenantIds)
      .NotNull()
      .WithMessage("Accepted tenant ids are required.");
    RuleFor(details => details.EntraAcceptedTenantIds)
      .Must(ids => ids is null || ids.Count <= EntraTenantIds.MaxAcceptedCount)
      .WithMessage($"At most {EntraTenantIds.MaxAcceptedCount} accepted tenant ids.");
    RuleForEach(details => details.EntraAcceptedTenantIds)
      .Must(tenantId => tenantId != Guid.Empty)
      .WithMessage("Accepted tenant ids must be non-empty GUIDs.");
    RuleFor(details => details.AssistantName)
      .NotEmpty()
      .WithMessage("Assistant name is required.");
    RuleFor(details => details.AssistantName)
      .Must(AssistantNameRules.IsValid)
      .When(static details => !string.IsNullOrWhiteSpace(details.AssistantName))
      .WithMessage
      (
        $"Assistant name must be 1-{AssistantNameRules.MaxLength} characters of plain text, "
        + "without markup, control, format or line-separator characters."
      );
  }
}
