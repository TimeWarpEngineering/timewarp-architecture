#region Purpose
// Endpoint-centric contract for starting a WebAuthn passkey registration ceremony.
#endregion

#region Design
// Empty body: the server does not need any client input to mint options — user.id (a random
// userHandle, not a persisted identifier) and the challenge are minted server-side per call.
// No GetMockResponseFactory: a WebAuthn ceremony cannot be meaningfully mocked without a browser
// credential to answer it (navigator.credentials.create needs real options JSON shaped by a real
// challenge that a later CompletePasskeyRegistration call must also satisfy) — SPA mock mode shows
// "passkeys not supported" instead (see web-spa/features/identity's PasskeysPage), a deliberate,
// documented opt-out rather than an oversight.
// [EndpointAllowAnonymous] (task 110): establishes the ceremony that will, if completed, create the
// session — there is no prior session to authorize against.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[ApiEndpoint]
[EndpointAllowAnonymous("Starts the passkey registration ceremony that will, if completed, create the session — no prior session exists to authorize against.")]
public static partial class StartPasskeyRegistration
{
  [ApiRoute("api/identity/passkey/register/options", HttpVerb.Post)]
  public sealed partial class Command : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Command>;

  public sealed class Response
  {
    public string OptionsJson { get; }

    public Response(string optionsJson)
    {
      OptionsJson = Guard.Against.NullOrEmpty(optionsJson);
    }
  }
}
