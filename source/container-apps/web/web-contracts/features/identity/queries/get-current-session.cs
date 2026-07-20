#region Purpose
// Endpoint-centric contract for reading the current browser session (set by a completed passkey
// ceremony) without deriving identity from a client-sent id.
#endregion

#region Design
// Route-only, empty body: identity comes from the request's session cookie (see
// IBrowserSessionService), never from a client-supplied UserId — this is a read of ambient
// authentication state, not a lookup by parameter.
// PrincipalId is nullable: IsAuthenticated=false pairs with PrincipalId=null (no session), and
// IsAuthenticated=true always carries a non-null PrincipalId. A round-1 review caught this region
// describing that pairing as an invariant while the ctor accepted any combination — enforced now:
// the ctor throws ArgumentException on isAuthenticated != (principalId is not null), so a
// disagreeing pair can never be constructed (and therefore never serialized), not merely one the
// handler happens to avoid producing.
// [EndpointAllowAnonymous] (task 110): reads whatever ambient session exists, if any — this IS the
// read of unauthenticated-or-authenticated state (IsAuthenticated=false is a valid, expected
// response), not a protected resource that requires a session to reach.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[ApiEndpoint]
[EndpointAllowAnonymous("Reads whatever ambient session exists, if any — IsAuthenticated=false is a valid, expected response, not an error.")]
public static partial class GetCurrentSession
{
  [ApiRoute("api/identity/session", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>;

  public sealed class Response
  {
    public bool IsAuthenticated { get; }
    public PrincipalId? PrincipalId { get; }

    public Response(bool isAuthenticated, PrincipalId? principalId)
    {
      if (isAuthenticated != (principalId is not null))
      {
        throw new ArgumentException
        (
          "IsAuthenticated and PrincipalId must agree: IsAuthenticated=true requires a non-null PrincipalId, IsAuthenticated=false requires null.",
          nameof(principalId)
        );
      }

      IsAuthenticated = isAuthenticated;
      PrincipalId = principalId;
    }
  }
}
