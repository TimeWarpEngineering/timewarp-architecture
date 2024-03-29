﻿namespace TimeWarp.Architecture.Configuration;

/// <remarks>
/// This class has to be `internal` or it will automatically be registered
/// by AddValidatorsFromAssemblyContaining as scoped
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
