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
// The handler reads RoleId only, and the route id is the whole request (task 458-008 removed the
// unread UserId query parameter). OpenApiDeleteRequestBody omits the request body: FastEndpoints
// would otherwise publish a required JSON body for this DELETE.
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
  [ApiRoute("api/admin/roles/{RoleId:guid}", HttpVerb.Delete)]
  [NotAggregate("Removes the role; the request carries only the route id, so there is no root member to agree on.")]
  [Operation("roles.delete", Description = "Delete a role.", Group = "admin.roles")]
  public sealed partial class Command : IAuthApiRequest, IIdempotentCommand<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x.RoleId).NotEmpty();
    }
  }

  public sealed class Response;

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response();
  }
}
