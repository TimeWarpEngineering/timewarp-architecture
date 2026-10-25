#region Purpose
// Shared bindable shape for GetSiteSettings / UpdateSiteSettings (Entra policy, passkey prompt,
// assistant name).
#endregion

#region Design
// I*Details so the Settings Authentication section binds one shape. Version is not on this
// interface — it is the concurrency token on Command/Response, not a form field the user types.
// PasskeyPromptMode and EntraTenantAcceptanceMode use TimeWarp.Identity enums
// (JsonStringEnumConverter). Validator: both enums must be defined. Accepted tenants call
// EntraTenantIds.TryNormalize, the same check SiteSettings uses, so an empty Guid, a bad
// domain, the distinct-count cap, and duplicate collapse cannot drift. An empty list is valid
// and means Restricted accepts nobody. Null is refused. Task 306: this shape is the trust
// policy. Task 401: each entry is a tenant id plus an optional domain.
// Task 399: XaiByokMode is whose key Ask uses below the host key. Allow is the zero value, so a
// client that omits the field stays on Allow. Reseed does not write this field.
// Task 361 added AssistantName: required, and trimmed it must be 1-32 plain-text characters.
// Beyond NotEmpty (the required message) the validator calls AssistantNameRules.IsValid, the
// same check SiteSettings.RenameAssistant runs, so they cannot drift.
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
  List<EntraAcceptedTenant> EntraAcceptedTenants { get; set; }
  XaiByokMode XaiByokMode { get; set; }
}

public sealed class SiteSettingsDetailsValidator : AbstractValidator<ISiteSettingsDetails>
{
  public SiteSettingsDetailsValidator()
  {
    RuleFor(details => details.PasskeyPromptMode)
      .IsInEnum();
    RuleFor(details => details.EntraTenantAcceptanceMode)
      .IsInEnum();
    RuleFor(details => details.XaiByokMode)
      .IsInEnum();
    RuleFor(details => details.EntraAcceptedTenants)
      .NotNull()
      .WithMessage("Accepted tenant ids are required.");
    RuleFor(details => details.EntraAcceptedTenants)
      .Custom(static (ids, context) =>
      {
        if (ids is null)
        {
          return;
        }

        if (!EntraTenantIds.TryNormalize(ids, out _, out string? error))
        {
          context.AddFailure(error ?? "Accepted tenant ids are invalid.");
        }
      });
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
