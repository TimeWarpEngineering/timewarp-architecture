#region Purpose
// Endpoint-centric contract for starting an agent public-key registration ceremony — the
// browser-less analog of StartPasskeyRegistration.
#endregion

#region Design
// Empty body: the server does not need any client input to mint a challenge. Unlike WebAuthn, the
// challenge travels as an explicit top-level string field (base64url) rather than being embedded
// inside a browser-produced clientDataJSON — agents sign UTF8(prefix) ‖ challenge directly
// (AgentKeyProof.BuildSignedData) and echo the challenge value back verbatim in
// CompleteAgentKeyRegistration, so there is no JSON wrapper to extract it from.
// No GetMockResponseFactory: an agent-key ceremony cannot be meaningfully mocked without a real
// keypair to answer it, same rationale as StartPasskeyRegistration's Design region.
// [EndpointAllowAnonymous] (task 110): mints a one-time registration challenge — no prior identity
// exists to authorize against, and no human sponsor is required by design (task 104-004 requirement).
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[ApiEndpoint]
[EndpointAllowAnonymous("Mints a one-time registration challenge; no prior identity to authorize against, and no human sponsor is required by design.")]
public static partial class StartAgentKeyRegistration
{
  [ApiRoute("api/identity/agent/register/options", HttpVerb.Post)]
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
