#region Purpose
// Endpoint-centric contract for ending the browser identity-session (passkey cookie sign-out).
#endregion

#region Design
// Task 104-034: SPA default path is identity-session, not Entra. Sign-out must clear the server
// cookie via this endpoint rather than RemoteAuthenticatorView (which requires unregistered
// IRemoteAuthenticationService when UseEntra is false).
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
