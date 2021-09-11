namespace TimeWarp.Blazor.Configuration
{
  using FluentValidation;

  public class CosmosDbOptionsValidator : AbstractValidator<CosmosDbOptions>
  {
    public CosmosDbOptionsValidator()
    {
      RuleFor(aCosmosDbOptions => aCosmosDbOptions.EndPoint).NotEmpty();
      RuleFor(aCosmosDbOptions => aCosmosDbOptions.AccessKey).NotEmpty();

      RuleFor(aCosmosDbOptions => aCosmosDbOptions.DocumentToCheck).NotEmpty()
        .When(aCosmosDbOptions => aCosmosDbOptions.EnableMigration);
    }
  }
}
