#region Purpose
// Endpoint-centric contract for starting an agent access-token issuance ceremony — the browser-less
// analog of StartPasskeyAuthentication.
#endregion

#region Design
// Empty body, same shape as StartAgentKeyRegistration: a fresh challenge for a proof-of-possession
// ceremony that is domain-separated from registration (AgentKeyCeremonyType.TokenIssuance uses a
// different signed-data prefix, "TimeWarp.Identity.AgentKey.Token.v1:" — see AgentKeyProof's Design
// region). No allowCredentials/discoverable-credential concept here (unlike WebAuthn authentication):
// the agent already knows which KeyId it is claiming and supplies it explicitly in
// CompleteAgentTokenIssuance.
// No GetMockResponseFactory — see StartAgentKeyRegistration's Design region.
// [EndpointAllowAnonymous] (task 110): mints a one-time token-issuance challenge — the agent has
// not yet proven possession of a key, so there is nothing to authorize against.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[ApiEndpoint]
[EndpointAllowAnonymous("Mints a one-time token-issuance challenge; the agent has not yet proven possession of a key, so there is nothing to authorize against.")]
public static partial class StartAgentTokenIssuance
{
  [ApiRoute("api/identity/agent/token/options", HttpVerb.Post)]
  public sealed partial class Command : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Command>;

  public sealed class Response
  {
    public string Challenge { get; }

    public Response(string challenge)
    {
      Challenge = Guard.Against.NullOrEmpty(challenge);
    }
  }
}
