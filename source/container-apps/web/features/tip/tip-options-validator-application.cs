#region Purpose
// FluentValidation for TipOptions bound from configuration.
#endregion

#region Design
// When Enabled is false, payTo/network/price may be empty (surface is dark → 503 at runtime).
// When Enabled is true, require a valid payTo and non-empty network/price/resource/facilitator so
// ValidateOnStart fails closed in Development rather than discovering misconfig on first tip hit.
#endregion

namespace TimeWarp.Architecture.Features.Tip.Application;

using TimeWarp.X402;

public sealed class TipOptionsValidator : AbstractValidator<TipOptions>
{
  public TipOptionsValidator()
  {
    When(
      options => options.Enabled,
      () =>
      {
        RuleFor(options => options.PayTo)
          .Must(PayToValidator.IsValid)
          .WithMessage("TipOptions:PayTo must be a valid EVM receive address when enabled.");
        RuleFor(options => options.Network).NotEmpty();
        RuleFor(options => options.Price).NotEmpty();
        RuleFor(options => options.Resource).NotEmpty();
        RuleFor(options => options.FacilitatorBase).NotEmpty();
      });
  }
}
