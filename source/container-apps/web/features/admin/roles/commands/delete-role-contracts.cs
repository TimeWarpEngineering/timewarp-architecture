#region Purpose
// Endpoint-centric contract for deleting a role.
#endregion

#region Design
// [ApiRoute] drives source generation of the route members (hence partial); RoleId comes from
// the {RoleId:guid} route segment, not a hand-declared property.
// The empty Response exists to keep the uniform OneOf<Response, SharedProblemDetails>
// pipeline even though a delete has no payload — callers still get success/problem typing.
// GetMockResponseFactory lets the SPA's MockWebApiService serve this endpoint offline.
// [EndpointAuthorize] (task 110): see CreateRole's Design region for the full rationale — same
// policy, same "no admin/role model yet" scope boundary.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

[ApiEndpoint]
[EndpointAuthorize(Policy = "identity-session-authenticated")] // matches IdentitySessionDefaults.AuthenticatedPolicy
public static partial class DeleteRole
{
  [ApiRoute("api/Roles/{RoleId:guid}", HttpVerb.Delete)]
  public sealed partial class Command : IAuthApiRequest, IQueryStringRouteProvider, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }

    // DELETE carries no body, so UserId travels in the query string.
    public string GetRouteWithQueryString() =>
      $"{GetRoute()}?{this.GetQueryString(new NameValueCollection { { nameof(UserId), UserId.ToString() } })}";
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x.RoleId).NotEmpty();
      RuleFor(x => x).SetValidator(new AuthApiRequestValidator());
    }
  }

  public sealed class Response;

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response();
  }
}
