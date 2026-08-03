#region Purpose
// Startup-time FluentValidation guard for AbuseRateLimitOptions configuration binding.
#endregion

#region Design
// Wired through AddFluentValidatedOptions(...).ValidateOnStart() in web-server's
// Program.ConfigureSettings — misconfiguration crashes at boot rather than silent no-op or
// ArgumentOutOfRangeException inside the rate-limiter factory on first request.
// Public so web-server can pass the type as AddFluentValidatedOptions's generic argument
// (same accessibility rationale as WebAuthnOptionsValidator).
// Enabled=false still validates nested numbers so a later enable does not surprise operators.
#endregion

namespace TimeWarp.Architecture.Abuse;

using FluentValidation;

public sealed class AbuseRateLimitOptionsValidator : AbstractValidator<AbuseRateLimitOptions>
{
  public AbuseRateLimitOptionsValidator()
  {
    RuleFor(options => options.PrincipalRegistration).NotNull();
    RuleFor(options => options.PaymentChallenge).NotNull();
    RuleFor(options => options.PrincipalRegistration).SetValidator(new SlidingWindowLimitOptionsValidator());
    RuleFor(options => options.PaymentChallenge).SetValidator(new SlidingWindowLimitOptionsValidator());
  }
}

internal sealed class SlidingWindowLimitOptionsValidator : AbstractValidator<SlidingWindowLimitOptions>
{
  public SlidingWindowLimitOptionsValidator()
  {
    RuleFor(options => options.PermitLimit).GreaterThan(0);
    RuleFor(options => options.WindowSeconds).GreaterThan(0);
    RuleFor(options => options.SegmentsPerWindow).GreaterThan(0);
  }
}
