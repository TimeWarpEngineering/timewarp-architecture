#region Purpose
// Startup-time FluentValidation guard for Authentication:Entra when the named scheme is enabled.
#endregion

#region Design
// When Enabled is false the section may be empty placeholders — do not fail boot. When Enabled,
// require Instance, TenantId, ClientId, and CallbackPath so AddOpenIdConnect is not registered
// with a blank authority. ClientSecret is not required here (user secrets / Key Vault).
// TrustedTenants entries that are present must be GUIDs; emptiness is allowed (fail-closed at
// ticket time — no tenant may bootstrap).
// PublicOrigin is optional (empty = request-derived redirect_uri). When set it must be an absolute
// https URI, or http only for localhost — never a non-https public host, and never a path/query
// (it is an origin, not the full redirect URI). Validated even when Enabled is false so a bad
// value cannot sit dormant until Entra is turned on.
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

    RuleFor(options => options.PublicOrigin)
      .Must(BeValidPublicOrigin)
      .When(options => !string.IsNullOrWhiteSpace(options.PublicOrigin))
      .WithMessage("Authentication:Entra:PublicOrigin must be an absolute https URI (http is allowed only for localhost).");
  }

  private static bool BeValidPublicOrigin(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return true;
    }

    if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out Uri? uri))
    {
      return false;
    }

    if (!string.IsNullOrEmpty(uri.UserInfo)
      || !string.IsNullOrEmpty(uri.Query)
      || !string.IsNullOrEmpty(uri.Fragment))
    {
      return false;
    }

    if (uri.AbsolutePath is not ("/" or ""))
    {
      return false;
    }

    if (uri.Scheme == Uri.UriSchemeHttps)
    {
      return true;
    }

    return uri.Scheme == Uri.UriSchemeHttp && IsLocalhost(uri.Host);
  }

  private static bool IsLocalhost(string host) =>
    string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
    || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase);
}
