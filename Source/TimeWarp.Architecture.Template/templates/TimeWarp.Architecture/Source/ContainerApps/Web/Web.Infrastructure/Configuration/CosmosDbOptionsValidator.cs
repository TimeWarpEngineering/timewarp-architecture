namespace TimeWarp.Architecture.Configuration;

using FluentValidation;

/// <remarks>
/// This class has to be `internal` or it will automatically be registered
/// by RegisterValidatorsFromAssemblyContaining as scoped
/// </remarks>
internal class CosmosDbOptionsValidator : AbstractValidator<CosmosDbOptions>
{
  public CosmosDbOptionsValidator()
  {
    RuleFor(aCosmosDbOptions => aCosmosDbOptions.Endpoint).NotEmpty();
    RuleFor(aCosmosDbOptions => aCosmosDbOptions.AccessKey).NotEmpty();

    RuleFor(aCosmosDbOptions => aCosmosDbOptions.DocumentToCheck).NotEmpty()
      .When(aCosmosDbOptions => aCosmosDbOptions.EnableMigration);
  }
}
