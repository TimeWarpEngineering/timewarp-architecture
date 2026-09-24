#region Purpose
// Endpoint-centric contract for ending the browser identity-session (passkey cookie sign-out).
#endregion

#region Design
// Task 104-034 / RFC 219 D10: SPA session is identity-session, not WASM MSAL. Sign-out clears the
// server cookie via this command's handler rather than RemoteAuthenticatorView.
// Task 251: the SPA no longer calls this JSON endpoint — under InteractiveServer that call is a
// server loopback whose Set-Cookie never reaches the browser. The SPA signs out through the
// browser-navigated SignOutBrowserSessionEndpoint, which dispatches THIS Command (one session-end
// implementation). The JSON endpoint stays for non-browser cookie-session clients (scripted
// clients, integration tests) that hold their own cookie jar; agent callers use bearer tokens and
// have no session to end.
// POST + /end (not DELETE): idempotent "end session" verb; empty body. AllowAnonymous so a
// double-click or already-expired cookie still returns success — never 401 on logout.
// Does not demote TrustTier or revoke credentials — only the ambient browser session.
#endregion

namespace TimeWarp.Architecture.Features.Identity;
[ApiEndpoint]
[EndpointAllowAnonymous("Sign-out is idempotent; missing/expired session is a success no-op.")]
public static partial class EndBrowserSession
{
  [ApiRoute("api/identity/session/end", HttpVerb.Post)]
  public sealed partial class Command : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Command>;

  public sealed class Response;

  public static MockResponseFactory<Response> GetMockResponseFactory() =>
    static _ => new Response();
}
