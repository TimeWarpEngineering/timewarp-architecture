#region Purpose
// Startup-time FluentValidation guard for Authentication:Entra when the named scheme is enabled.
#endregion

#region Design
// When Enabled is false the section may be empty placeholders — do not fail boot. When Enabled,
// require Instance, TenantId, ClientId, and CallbackPath so AddOpenIdConnect is not registered
// with a blank authority. ClientSecret is not required here (user secrets / Key Vault). 
// TrustedTenants entries that are present must be GUIDs; emptiness is allowed (fail-closed at
// ticket time — no tenant may bootstrap).
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

public sealed class EntraAuthenticationOptionsValidator : AbstractValidator<EntraAuthenticationOptions>
{
  public EntraAuthenticationOptionsValidator()
  {
    When
    (
      options => options.Enabled,
      () =>
      {
        RuleFor(options => options.Instance).NotEmpty();
        RuleFor(options => options.TenantId).NotEmpty();
        RuleFor(options => options.ClientId).NotEmpty();
        RuleFor(options => options.CallbackPath).NotEmpty();
      }
    );

    RuleForEach(options => options.TrustedTenants)
      .Must(value => Guid.TryParse(value, out _))
      .When(options => options.TrustedTenants.Count > 0)
      .WithMessage("Authentication:Entra:TrustedTenants entries must be GUIDs.");
  }
}
