namespace TimeWarp.Architecture.Configuration;

/// <remarks>
/// This class has to be `internal` or it will automatically be registered
/// by AddValidatorsFromAssemblyContaining as scoped
/// </remarks>
internal class ServiceCollectionOptionsValidator : AbstractValidator<ServiceCollectionOptions>
{
  public ServiceCollectionOptionsValidator()
  {
#if grpc
    RuleFor(aServiceCollectionOptions => aServiceCollectionOptions)
      .Must(aServiceCollectionOptions => aServiceCollectionOptions.ContainsKey(Constants.GrpcServiceName))
      .WithMessage($"The {Constants.GrpcServiceName} service must be configured.");

    RuleFor(aServicesOptions => aServicesOptions[Constants.GrpcServiceName])
      .SetValidator(new ServiceValidator(Constants.GrpcServiceName))
      .When(aServiceCollectionOptions => aServiceCollectionOptions.ContainsKey(Constants.GrpcServiceName));
#endif
    //// Api Service
    //RuleFor(aServiceCollectionOptions => aServiceCollectionOptions)
    //      .Must(aServiceCollectionOptions => aServiceCollectionOptions.ContainsKey(Constants.ApiServiceName))
    //      .WithMessage($"The {Constants.ApiServiceName} service must be configured.");

    //RuleFor(aServicesOptions => aServicesOptions[Constants.ApiServiceName])
    //  .SetValidator(new ServiceValidator(Constants.ApiServiceName))
    //  .When(aServiceCollectionOptions => aServiceCollectionOptions.ContainsKey(Constants.ApiServiceName));

    //// Web Service
    //RuleFor(aServiceCollectionOptions => aServiceCollectionOptions)
    //      .Must(aServiceCollectionOptions => aServiceCollectionOptions.ContainsKey(Constants.WebServiceName))
    //      .WithMessage($"The {Constants.WebServiceName} service must be configured.");

    //RuleFor(aServicesOptions => aServicesOptions[Constants.WebServiceName])
    //  .SetValidator(new ServiceValidator(Constants.WebServiceName))
    //  .When(aServiceCollectionOptions => aServiceCollectionOptions.ContainsKey(Constants.WebServiceName));
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

