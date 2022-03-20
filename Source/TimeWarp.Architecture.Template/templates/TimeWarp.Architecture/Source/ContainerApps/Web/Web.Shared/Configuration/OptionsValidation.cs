// #Settings #Configuration #Core
namespace TimeWarp.Architecture.Configuration
{
  using FluentValidation;
  using FluentValidation.Results;
  using Microsoft.Extensions.Options;
  using System.Linq;
  public class OptionsValidation<TOptions, TOptionsValidator> : IValidateOptions<TOptions>
    where TOptions : class
    where TOptionsValidator : AbstractValidator<TOptions>
  {
    private readonly TOptionsValidator OptionsValidator;

    public OptionsValidation
    (
      TOptionsValidator aOptionsValidator
    )
    {
      OptionsValidator = aOptionsValidator;
    }

    public ValidateOptionsResult Validate(string aName, TOptions aOptions)
    {
      ValidationResult validationResult = OptionsValidator.Validate(aOptions);
      if (validationResult.IsValid)
      {
        return ValidateOptionsResult.Success;
      }

      return ValidateOptionsResult.Fail
      (
        validationResult.Errors.Select(aValidationFailure => aValidationFailure.ErrorMessage)
      );

    }
  }
}
