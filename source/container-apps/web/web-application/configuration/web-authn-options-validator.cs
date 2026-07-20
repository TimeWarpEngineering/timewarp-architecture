#region Purpose
// Startup-time FluentValidation guard for WebAuthnOptions configuration binding.
#endregion

#region Design
// Wired through AddFluentValidatedOptions(...).ValidateOnStart() in web-server's
// Program.ConfigureSettings, so misconfiguration crashes the host at boot instead of failing on the
// first ceremony request.
// Public, unlike SampleOptionsValidator/foundation's internal-options-validator convention: this
// type must be referenceable BY NAME from web-server (a different assembly) to pass as
// AddFluentValidatedOptions's generic type argument. The "keep it internal so
// AddValidatorsFromAssemblyContaining does not auto-register it as a scoped request validator"
// rationale that justifies `internal` elsewhere does not apply here — web-server only scans its own
// assembly and web-contracts's for auto-registration (see Program.ConfigureServices), never
// web-application's, so this validator was never at risk of double-registration regardless of
// accessibility.
// AllowedOrigins is not validated for emptiness — an empty list is the deliberate dev-fallback
// posture (see WebAuthnOptions's Design region), not a misconfiguration.
#endregion

namespace TimeWarp.Architecture.Configuration;

public class WebAuthnOptionsValidator : AbstractValidator<WebAuthnOptions>
{
  public WebAuthnOptionsValidator()
  {
    RuleFor(webAuthnOptions => webAuthnOptions.RpId).NotEmpty();
    RuleFor(webAuthnOptions => webAuthnOptions.RpName).NotEmpty();
  }
}
