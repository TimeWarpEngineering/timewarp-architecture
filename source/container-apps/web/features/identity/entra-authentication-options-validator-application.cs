#region Purpose
// Startup-time FluentValidation guard for Authentication:Entra when the named scheme is enabled.
#endregion

#region Design
// When Enabled is false the section may be empty placeholders — do not fail boot. When Enabled,
// require Instance, TenantId, ClientId, and CallbackPath so AddOpenIdConnect is not registered
// with a blank authority. ClientSecret is not required here (user secrets / Key Vault).
// Task 227 removed TrustedTenants; if that key is still present in configuration the validator
// fails with a message that names TenantId as the pin. organizations / common TenantId is still
// a valid *string* at bind time (template placeholder when Enabled is false); ticket policy
// refuses those authorities with Untrusted tenant.
// PublicOrigin is optional (empty = request-derived redirect_uri). When set it must be an absolute
// https URI, or http only for localhost — never a non-https public host, and never a path/query
// (it is an origin, not the full redirect URI). Validated even when Enabled is false so a bad
// value cannot sit dormant until Entra is turned on.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

public sealed class EntraAuthenticationOptionsValidator : AbstractValidator<EntraAuthenticationOptions>
{
  public const string TrustedTenantsRemovedMessage =
    "TrustedTenants was removed by task 227; trust is TenantId";

  public EntraAuthenticationOptionsValidator()
    : this(configuration: null)
  {
  }

  public EntraAuthenticationOptionsValidator(IConfiguration? configuration)
  {
    if (configuration is not null)
    {
      RuleFor(options => options)
        .Must(_ => !HasObsoleteTrustedTenants(configuration))
        .WithMessage(TrustedTenantsRemovedMessage);
    }

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

    RuleFor(options => options.PublicOrigin)
      .Must(BeValidPublicOrigin)
      .When(options => !string.IsNullOrWhiteSpace(options.PublicOrigin))
      .WithMessage("Authentication:Entra:PublicOrigin must be an absolute https URI (http is allowed only for localhost).");
  }

  internal static bool HasObsoleteTrustedTenants(IConfiguration configuration)
  {
    foreach (KeyValuePair<string, string?> pair in configuration.AsEnumerable())
    {
      if (pair.Key.StartsWith($"{EntraAuthenticationOptions.SectionKey}:TrustedTenants", StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }
    }

    return configuration.GetSection(EntraAuthenticationOptions.SectionKey)
      .GetChildren()
      .Any(static child => string.Equals(child.Key, "TrustedTenants", StringComparison.OrdinalIgnoreCase));
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

/// <summary>
/// Surfaces FluentValidation failures (including the removed TrustedTenants key) at options start.
/// </summary>
public sealed class EntraAuthenticationOptionsStartupValidator : IValidateOptions<EntraAuthenticationOptions>
{
  private readonly IConfiguration Configuration;

  public EntraAuthenticationOptionsStartupValidator(IConfiguration configuration)
  {
    Configuration = configuration;
  }

  public ValidateOptionsResult Validate(string? name, EntraAuthenticationOptions options)
  {
    FluentValidation.Results.ValidationResult result =
      new EntraAuthenticationOptionsValidator(Configuration).Validate(options);
    if (result.IsValid)
    {
      return ValidateOptionsResult.Success;
    }

    return ValidateOptionsResult.Fail(result.Errors.Select(static error => error.ErrorMessage));
  }
}
