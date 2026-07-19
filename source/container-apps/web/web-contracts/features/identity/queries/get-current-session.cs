#region Purpose
// Endpoint-centric contract for reading the current browser session (set by a completed passkey
// ceremony) without deriving identity from a client-sent id.
#endregion

#region Design
// Route-only, empty body: identity comes from the request's session cookie (see
// IBrowserSessionService), never from a client-supplied UserId — this is a read of ambient
// authentication state, not a lookup by parameter.
// PrincipalId is nullable: IsAuthenticated=false pairs with PrincipalId=null (no session); the two
// fields are set together by the handler so they can never disagree from a caller's perspective.
// No invariant to guard in the ctor beyond that pairing (PrincipalId, unlike the other identity
// contracts' Responses, is legitimately allowed to be empty/absent here), so a plain ctor without
// Guard.Against suffices.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

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
      IsAuthenticated = isAuthenticated;
      PrincipalId = principalId;
    }
  }
}
