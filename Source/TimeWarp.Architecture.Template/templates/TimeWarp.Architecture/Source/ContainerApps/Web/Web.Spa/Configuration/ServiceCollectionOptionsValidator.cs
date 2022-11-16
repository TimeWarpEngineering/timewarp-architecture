namespace TimeWarp.Architecture.Configuration;

using FluentValidation;

/// <remarks>
/// This class has to be `internal` or it will automatically be registered
/// by AddValidatorsFromAssemblyContaining as scoped
/// </remarks>
internal class ServiceCollectionOptionsValidator : AbstractValidator<ServiceCollectionOptions>
{
  public ServiceCollectionOptionsValidator()
  {

  }

  internal class ServiceValidator : AbstractValidator<ServiceCollectionOptions.Service>
  {
    private readonly string ServiceName;
    public ServiceValidator(string aServiceName)
    {
      ServiceName = aServiceName;
      RuleFor(aService => aService.Host)
        .NotEmpty()
        .WithMessage($"{nameof(ServiceCollectionOptions.Service.Host)} is required for {nameof(ServiceCollectionOptions.Service)}:{ServiceName}");

      RuleFor(aService => aService.Port)
        .GreaterThan(0)
        .WithMessage($"{nameof(ServiceCollectionOptions.Service.Port)} must be greater than 0 for {nameof(ServiceCollectionOptions.Service)}:{ServiceName}");

      RuleFor(aService => aService.Protocol)
        .NotEmpty()
        .WithMessage($"{nameof(ServiceCollectionOptions.Service.Protocol)} is required for {nameof(ServiceCollectionOptions.Service)}: {ServiceName}");
    }
  }
}

