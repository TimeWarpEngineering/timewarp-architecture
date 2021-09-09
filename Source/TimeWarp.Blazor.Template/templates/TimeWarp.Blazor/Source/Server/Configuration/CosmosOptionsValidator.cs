namespace TimeWarp.Blazor.Configuration
{
  using FluentValidation;

  public class CosmosOptionsValidator : AbstractValidator<CosmosOptions>
  {
    public CosmosOptionsValidator()
    {
      RuleFor(aCosmosOptions => aCosmosOptions.EndPoint).NotEmpty();
      RuleFor(aCosmosOptions => aCosmosOptions.AccessKey).NotEmpty();
      RuleFor(aCosmosOptions => aCosmosOptions.EndPoint).NotEmpty();
      RuleFor(aCosmosOptions => aCosmosOptions.EndPoint).NotEmpty();
    }
  }
}
