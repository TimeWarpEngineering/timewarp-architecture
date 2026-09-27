#region Purpose
// Endpoint-centric contract for starting a WebAuthn passkey registration ceremony.
#endregion

#region Design
// One optional flag, no identity input: user.id (a random userHandle, not a persisted
// identifier) and the challenge are minted server-side per call, and the WebAuthn user name
// ("TimeWarp account · <account fingerprint>", task 253 — see PasskeyAccountName) is derived
// server-side too. ForCurrentAccount only picks WHICH account the name describes:
//   - false (default; Login / new-account create): the server pre-allocates the PrincipalId that
//     CompletePasskeyRegistration will mint and keeps it with the challenge — never on the wire.
//   - true (Settings "Create a passkey" → AddPasskey): the name is the signed-in caller's account,
//     resolved from the session cookie; no session → 401. The flag cannot select someone else's
//     account — the caller's id always comes from the session.
// No GetMockResponseFactory: a WebAuthn ceremony cannot be meaningfully mocked without a browser
// credential to answer it (navigator.credentials.create needs real options JSON shaped by a real
// challenge that a later CompletePasskeyRegistration call must also satisfy) — SPA mock mode shows
// "passkeys not supported" instead (see web-spa/features/identity's PasskeysPage), a deliberate,
// documented opt-out rather than an oversight.
// [EndpointAllowAnonymous] (task 110): establishes the ceremony that will, if completed, create the
// session — there is no prior session to authorize against. ForCurrentAccount=true is the one case
// that needs a session; the handler reads the cookie itself and 401s without one (a naming input,
// not a protected resource — the attach itself stays behind AddPasskey's [EndpointAuthorize]).
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[ApiEndpoint]
[EndpointAllowAnonymous("Starts the passkey registration ceremony that will, if completed, create the session — no prior session exists to authorize against.")]
public static partial class StartPasskeyRegistration
{
  [ApiRoute("api/identity/passkey/register/options", HttpVerb.Post)]
  public sealed partial class Command : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    /// <summary>True when the passkey will be added to the signed-in account (AddPasskey), false for a new account.</summary>
    public bool ForCurrentAccount { get; set; }
  }

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
