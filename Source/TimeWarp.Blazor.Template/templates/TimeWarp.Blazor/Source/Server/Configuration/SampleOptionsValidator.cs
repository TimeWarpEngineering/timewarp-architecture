namespace TimeWarp.Blazor.Configuration
{
  using FluentValidation;

  public class SampleOptionsValidator : AbstractValidator<SampleOptions>
  {
    public SampleOptionsValidator()
    {
      RuleFor(aSampleOptions => aSampleOptions.SampleOption).NotEmpty();
    }
  }
}
