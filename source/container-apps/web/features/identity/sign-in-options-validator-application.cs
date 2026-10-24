#region Purpose
// Startup-time FluentValidation guard for SignInOptions.
#endregion

#region Design
// Wired through AddFluentValidatedOptions(...).ValidateOnStart() in web-server ConfigureSettings.
// Empty and duplicate lists fail here. Each bound value must be a defined SignInMethod. Raw
// configuration names that do not parse fail here too, so an unknown name cannot sit in the
// section while the binder leaves the initializer in place. IConfiguration is injected by
// TryAddSingleton of this validator. This validator does not read EntraAuthenticationOptions.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using Microsoft.Extensions.Configuration;

/// <summary>Validator for <see cref="SignInOptions"/>.</summary>
public sealed class SignInOptionsValidator : AbstractValidator<SignInOptions>
{
  public SignInOptionsValidator(IConfiguration configuration)
  {
    RuleFor(options => options.Methods)
      .NotNull()
      .Must(methods => methods!.Count > 0)
      .WithMessage("SignInOptions:Methods must list at least one sign-in method.");

    RuleFor(options => options.Methods)
      .Must(methods => methods is null || methods.Distinct().Count() == methods.Count)
      .WithMessage("SignInOptions:Methods must not contain duplicates.");

    RuleForEach(options => options.Methods).IsInEnum();

    RuleFor(options => options)
      .Must(_ => UnknownMethodNames(configuration).Count == 0)
      .WithMessage(_ =>
        $"SignInOptions:Methods has unknown name(s): {string.Join(", ", UnknownMethodNames(configuration))}.");
  }

  internal static IReadOnlyList<string> UnknownMethodNames(IConfiguration configuration)
  {
    IConfigurationSection section = configuration.GetSection($"{SignInOptions.SectionName}:Methods");
    if (!section.Exists())
    {
      return [];
    }

    List<string> unknown = [];
    foreach (IConfigurationSection child in section.GetChildren())
    {
      string value = child.Value ?? "";
      if (!Enum.TryParse(value, ignoreCase: false, out SignInMethod parsed) || !Enum.IsDefined(parsed))
      {
        unknown.Add(value);
      }
    }

    return unknown;
  }
}
