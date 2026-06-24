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
