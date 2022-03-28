namespace TimeWarp.Architecture.Configuration;

using FluentValidation;

/// <remarks>
/// This class has to be `internal` or it will automatically be registered
/// by RegisterValidatorsFromAssemblyContaining as scoped
/// </remarks>
internal class ServiceCollectionValidator : AbstractValidator<ServiceCollection>
{
  public ServiceCollectionValidator()
  {
    RuleFor(aServicesOptions => aServicesOptions)
      .NotEmpty()
      .Must(aServices => aServices.ContainsKey(Constants.GrpcServiceName))
      .WithMessage($"The {Constants.GrpcServiceName} service must be configured.")
      .Must(aServices => aServices.ContainsKey(Constants.ApiServiceName))
      .WithMessage($"The {Constants.ApiServiceName} service must be configured.")
      .Must
      (
        aServices =>
          aServices.ContainsKey(Constants.WebServiceName)
      )
      .WithMessage($"The {Constants.WebServiceName} service must be configured.");

    RuleForEach(aServicesOptions => aServicesOptions)
      .ChildRules
      (
        aServicesRule =>
        {
          aServicesRule.RuleFor(aService => aService.Value.Protocol)
          .NotEmpty()
          .WithMessage("Service[{CollectionIndex}].Protocol must be assigned.");

          aServicesRule.RuleFor(aService => aService.Value.Host)
            .NotEmpty()
            .WithMessage("Service[{CollectionIndex}].Host must be assigned.");

          aServicesRule.RuleFor(aService => aService.Value.Port)
            .GreaterThan(0)
            .WithMessage("Service[{CollectionIndex}].Port must be greater than '0' but was '{ComparisonValue}'.");
        }
      );
  }
}

//internal class ServiceValidator : AbstractValidator<Service>
//{
//  public ServiceValidator()
//  {
//    RuleFor(aService => aService.Host).NotEmpty();
//    RuleFor(aService => aService.Port).GreaterThan(0);
//    RuleFor(aService => aService.Protocol).NotEmpty();
//  }
//}
