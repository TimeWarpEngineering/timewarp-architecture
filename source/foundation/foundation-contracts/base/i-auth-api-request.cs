#region Purpose
// Marks a request whose endpoint serves an authenticated caller.
#endregion

#region Design
// Task 458-008: the interface carries no members. It used to carry a required Guid UserId that
// published as a required query or body field on every auth contract, while no hosted handler read
// it: the server takes the caller from the session or bearer principal (ICurrentPrincipalAccessor)
// and [EndpointAuthorize] secures the route. A generated client had to invent a Guid to call these
// endpoints, so UserId left the wire and AuthApiRequestValidator went with it. A request never names
// its caller; handlers read the principal.
// The marker stays so EndpointAuthPostureAnalyzer (TWA0014) can flag an [EndpointAllowAnonymous]
// contract whose Query/Command says it serves an authenticated caller.
// A client-only contract that needs a user id for mock responses declares its own property
// (GetCurrentUser.Query.UserId); it never reaches a server.
#endregion

namespace TimeWarp.Foundation.Features;

/// <summary>
/// API request whose endpoint serves an authenticated caller. The server reads the caller from the
/// session or bearer principal; the request does not carry it.
/// </summary>
public interface IAuthApiRequest : IApiRequest;
