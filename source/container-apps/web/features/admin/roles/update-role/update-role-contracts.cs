#region Purpose
// Endpoint-centric contract for updating a role.
#endregion

#region Design
// The RoleId the Validator references is not declared here: the {RoleId:guid} segment in
// [ApiRoute] makes the source generator emit it on the partial Command, keeping route and
// body in one type. IRoleDetails lets the Validator compose the shared RoleDetailsValidator
// so update enforces the same field rules as create. GetMockResponseFactory lets the SPA's
// MockWebApiService serve this endpoint offline; the empty Response preserves OneOf typing.
// [EndpointAuthorize] (task 182-002): admin.roles.manage (PermissionIds); read is GetRoles/GetRole.
// AuthenticationSchemes (task 158): identity-session + mock-identity-session — see
// AuthenticationSchemeNames' Design region.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.AdminRolesManage,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class UpdateRole
{
  [ApiRoute("api/Roles/{RoleId:guid}", HttpVerb.Put)]
  public sealed partial class Command : IAuthApiRequest, IRoleDetails, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x.RoleId).NotEmpty();
      RuleFor(x => x).SetValidator(new RoleDetailsValidator());
      RuleFor(x => x).SetValidator(new AuthApiRequestValidator());
    }
  }

  public sealed class Response;

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response();
  }
}
