#region Purpose
// FluentValidation for MeteredCapabilityOptions bound from configuration.
#endregion

#region Design
// When Enabled is false, payTo/network/price may be empty (surface is dark → 503 at runtime).
// When Enabled is true, require a valid payTo and non-empty network/price/resource/facilitator so
// ValidateOnStart fails closed in Development rather than discovering misconfig on first 402 path.
#endregion

namespace TimeWarp.Architecture.Features.MeteredCapability.Application;

using TimeWarp.X402;

public sealed class MeteredCapabilityOptionsValidator : AbstractValidator<MeteredCapabilityOptions>
{
  public MeteredCapabilityOptionsValidator()
  {
    When(
      options => options.Enabled,
      () =>
      {
        RuleFor(options => options.PayTo)
          .Must(PayToValidator.IsValid)
          .WithMessage("MeteredCapability:PayTo must be a valid EVM receive address when enabled.");
        RuleFor(options => options.Network).NotEmpty();
        RuleFor(options => options.Price).NotEmpty();
        RuleFor(options => options.Resource).NotEmpty();
        RuleFor(options => options.FacilitatorBase).NotEmpty();
      });
  }
}
