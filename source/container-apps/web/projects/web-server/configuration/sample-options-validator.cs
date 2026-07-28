#region Purpose
// Startup-time FluentValidation guard for SampleOptions configuration binding.
#endregion

#region Design
// Wired through AddFluentValidatedOptions(...).ValidateOnStart() in Program.ConfigureSettings,
// so misconfiguration crashes the host at boot instead of failing on first use.
// Internal visibility is load-bearing: it keeps the validator out of the scoped
// AddValidatorsFromAssemblyContaining auto-registration used for request validators.
#endregion

namespace TimeWarp.Architecture.Configuration;

/// <summary>
/// Validator for <see cref="SampleOptions"/>.
/// </summary>
/// <remarks>
/// This class has to be `internal` or it will automatically be registered
/// by AddValidatorsFromAssemblyContaining as scoped
/// </remarks>
internal class SampleOptionsValidator : AbstractValidator<SampleOptions>
{
  public SampleOptionsValidator()
  {
    RuleFor(sampleOptions => sampleOptions.SampleOption).NotEmpty();
  }
}
