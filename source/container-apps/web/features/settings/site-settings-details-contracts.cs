#region Purpose
// Shared bindable shape for GetSiteSettings / UpdateSiteSettings (Entra policy and passkey prompt).
#endregion

#region Design
// I*Details so the Settings Authentication section binds one shape. Version is not on this
// interface — it is the concurrency token on Command/Response, not a form field the user types.
// UpdateSiteSettings.Command implements the generated CopyBeyondDetails hook to copy Version.
// PasskeyPromptMode and EntraTenantAcceptanceMode use TimeWarp.Identity enums
// (JsonStringEnumConverter). Validator: both enums must be defined. Accepted tenants call
// EntraTenantIds.TryNormalize, the same check SiteSettings uses, so an empty Guid, a bad
// domain, the distinct-count cap, and duplicate collapse cannot drift. An empty list is valid
// and means Restricted accepts nobody. Null is refused. Task 306: this shape is the trust
// policy. Task 401: each entry is a tenant id plus an optional domain.
// AssistantName is not on this shape. UpdateAssistantName owns the name so an authentication
// save does not round-trip it. XaiByokMode is not on this shape either. UpdateXaiByokMode owns
// the mode so an authentication save cannot overwrite it. GetSiteSettings.Response implements
// IAssistantNameDetails and IXaiByokModeDetails so those commands copy from the whole response.
// UpdateSiteSettings.Response still echoes the stored name and the stored mode.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using TimeWarp.Identity;

public interface ISiteSettingsDetails
{
  /// <summary>Whether Microsoft 365 sign-in is offered.</summary>
  bool EntraSignInEnabled { get; set; }
  /// <summary>Whether a Microsoft 365 sign-in may create the first account.</summary>
  bool EntraAllowBootstrap { get; set; }
  /// <summary>When users are prompted to add a passkey.</summary>
  PasskeyPromptMode PasskeyPromptMode { get; set; }
  /// <summary>Which Entra tenants may sign in.</summary>
  EntraTenantAcceptanceMode EntraTenantAcceptanceMode { get; set; }
  /// <summary>Tenants accepted when the acceptance mode is Restricted.</summary>
  List<EntraAcceptedTenant> EntraAcceptedTenants { get; set; }
}

public sealed class SiteSettingsDetailsValidator : AbstractValidator<ISiteSettingsDetails>
{
  public SiteSettingsDetailsValidator()
  {
    RuleFor(details => details.PasskeyPromptMode)
      .IsInEnum();
    RuleFor(details => details.EntraTenantAcceptanceMode)
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
  }
}
