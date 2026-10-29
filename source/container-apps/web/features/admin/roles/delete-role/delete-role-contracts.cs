#region Purpose
// Endpoint-centric contract for deleting a role.
#endregion

#region Design
// [ApiRoute] drives source generation of the route members (hence partial); RoleId comes from
// the {RoleId:guid} route segment, not a hand-declared property.
// The empty Response exists to keep the uniform OneOf<Response, SharedProblemDetails>
// pipeline even though a delete has no payload — callers still get success/problem typing.
// GetMockResponseFactory lets the SPA's MockWebApiService serve this endpoint offline.
// [EndpointAuthorize] (task 182-002): admin.roles.manage (PermissionIds); read is GetRoles/GetRole.
// AuthenticationSchemes (task 158): identity-session + mock-identity-session so the generated
// FastEndpoint's AuthSchemes(...) invokes the mock handler under closed-box tests — see
// AuthenticationSchemeNames' Design region for why.
// [Operation] names it roles.delete for every transport. IIdempotentCommand: a retried delete
// leaves the role gone; the second call may report not found but changes nothing.
// The handler reads RoleId only. UserId is the mock-mode caller id on the query string
// (IQueryStringRouteProvider). OpenApiDeleteRequestBody publishes that query parameter and
// omits a request body: FastEndpoints would otherwise require a JSON body for this DELETE.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.AdminRolesManage,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class DeleteRole
{
  [ApiRoute("api/Roles/{RoleId:guid}", HttpVerb.Delete)]
  [Operation("roles.delete", Description = "Delete a role.", Group = "admin.roles")]
  public sealed partial class Command : IAuthApiRequest, IQueryStringRouteProvider, IIdempotentCommand<OneOf<Response, SharedProblemDetails>>
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
