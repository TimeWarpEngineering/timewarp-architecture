namespace TimeWarp.Architecture.Configuration;

using FluentValidation;

/// <remarks>
/// This class has to be `internal` or it will automatically be registered
/// by RegisterValidatorsFromAssemblyContaining as scoped
/// </remarks>
internal class ServiceCollectionOptionsValidator : AbstractValidator<ServiceCollectionOptions>
{
  public ServiceCollectionOptionsValidator()
  {
    RuleFor(aServicesOptions => aServicesOptions[Constants.GrpcServiceName])
      .SetValidator(new ServiceValidator(Constants.GrpcServiceName));

    //RuleFor(aServicesOptions => aServicesOptions[Constants.ApiServiceName])
    //  .SetValidator(new ServiceValidator(Constants.ApiServiceName));

    //RuleFor(aServicesOptions => aServicesOptions[Constants.WebServiceName])
    //  .SetValidator(new ServiceValidator(Constants.WebServiceName));
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

