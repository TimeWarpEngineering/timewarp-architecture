#region Purpose
// Startup-time FluentValidation guard for AgentTokenOptions configuration binding.
#endregion

#region Design
// Wired through AddFluentValidatedOptions(...).ValidateOnStart() in web-server's
// Program.ConfigureSettings, so misconfiguration crashes the host at boot instead of failing on the
// first token-issuance request.
// Public, matching WebAuthnOptionsValidator's precedent (not the internal-to-dodge-auto-registration
// convention SampleOptionsValidator uses): this type must be referenceable BY NAME from web-server
// (a different assembly) to pass as AddFluentValidatedOptions's generic type argument. web-server's
// AddValidatorsFromAssemblyContaining calls only scan the web-server and web-contracts assemblies
// (see program.cs), never web-application, so this validator is not at risk of double-registration
// as a scoped request validator.
// 1–60 minutes: 1 as a sane floor (anything shorter is not meaningfully different from "the request
// itself," and would make normal request latency a real expiry risk); 60 as a generous ceiling for a
// short-lived-by-design token — this is not meant to become a long-lived credential.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

public class AgentTokenOptionsValidator : AbstractValidator<AgentTokenOptions>
{
  public AgentTokenOptionsValidator()
  {
    RuleFor(agentTokenOptions => agentTokenOptions.TokenLifetimeMinutes).InclusiveBetween(1, 60);
  }
}
