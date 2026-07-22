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
// AllowedRpIds (task 104-031) IS validated for emptiness — an app that serves passkeys under NO RP
// ID is a misconfiguration, not a posture (every request would fail closed). Each entry must be a
// bare DNS host name: Uri.CheckHostName rejects scheme/port/path/empty/IP-literals (an RP ID is a
// domain, never "https://host", "host:443", or "127.0.0.1"). No duplicate-entry rule: binder append
// semantics (see WebAuthnOptions's Design region) can legitimately produce ["localhost","localhost"]
// if config is authored carelessly, and a duplicate is harmless to selection (first match wins);
// failing the host on a duplicate would be a surprising boot crash for a non-security nit.
#endregion

namespace TimeWarp.Architecture.Configuration;

public class WebAuthnOptionsValidator : AbstractValidator<WebAuthnOptions>
{
  public WebAuthnOptionsValidator()
  {
    RuleFor(webAuthnOptions => webAuthnOptions.AllowedRpIds).NotEmpty();
    RuleForEach(webAuthnOptions => webAuthnOptions.AllowedRpIds)
      .Must(entry => Uri.CheckHostName(entry) == UriHostNameType.Dns)
      .WithMessage("Each AllowedRpIds entry must be a bare DNS host name (no scheme, port, path, or IP literal).");
    RuleFor(webAuthnOptions => webAuthnOptions.RpName).NotEmpty();
  }
}
