namespace TimeWarp.Architecture.Configuration;

using FluentValidation;

/// <remarks>
/// This class has to be `internal` or it will automatically be registered
/// by AddValidatorsFromAssemblyContaining as scoped
/// </remarks>
internal class SampleOptionsValidator : AbstractValidator<SampleOptions>
{
  public SampleOptionsValidator()
  {
    RuleFor(aSampleOptions => aSampleOptions.SampleOption).NotEmpty();
  }
}
