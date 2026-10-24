#region Purpose
// Startup-time FluentValidation guard for SignInOptions.
#endregion

#region Design
// Wired through AddFluentValidatedOptions(...).ValidateOnStart() in the SPA ConfigureSettings,
// which the server host calls too, so an unknown mode crashes the host at boot.
// Internal visibility keeps the validator out of AddValidatorsFromAssemblyContaining.
// IsInEnum rejects a numeric value the binder accepts but the enum does not define.
#endregion

namespace TimeWarp.Architecture.Configuration;

/// <summary>Validator for <see cref="SignInOptions"/>.</summary>
internal sealed class SignInOptionsValidator : AbstractValidator<SignInOptions>
{
  public SignInOptionsValidator()
  {
    RuleFor(options => options.Mode).IsInEnum();
  }
}
