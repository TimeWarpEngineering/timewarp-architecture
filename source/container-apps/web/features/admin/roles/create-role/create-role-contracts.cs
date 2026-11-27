#region Purpose
// Endpoint-centric contract for creating a role.
#endregion

#region Design
// [ApiRoute] drives source generation of the FastEndpoint and the Command's route members
// (hence partial). Implementing IRoleDetails lets the Validator compose the shared
// RoleDetailsValidator, so create and update forms enforce identical rules.
// GetMockResponseFactory lets the SPA's MockWebApiService serve this endpoint with no
// backend running; the response echoes a well-known RoleIds constant for determinism.
// [EndpointAuthorize] (task 182-002): admin.roles.manage (PermissionIds) — write half of the
// roles split; list/get use admin.roles.read. This attribute gates the server.
// AuthenticationSchemes (task 158): identity-session + mock-identity-session — see
// AuthenticationSchemeNames' Design region.
// [Operation] names it roles.create for every transport. ICommand, not IIdempotentCommand:
// a retried create makes a second role.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.AdminRolesManage,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class CreateRole
{
  [ApiRoute("api/admin/roles", HttpVerb.Post)]
  [Operation("roles.create", Description = "Create a role.", Group = "admin.roles")]
  public sealed partial class Command : IAuthApiRequest, IRoleDetails, ICommand<OneOf<Response, SharedProblemDetails>>
  {
    /// <summary>The role's display name.</summary>
    public string Name { get; set; } = null!;
    /// <summary>What the role is for.</summary>
    public string Description { get; set; } = null!;
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x).SetValidator(new RoleDetailsValidator());
    }
  }

  public sealed class Response
  {
    /// <summary>The identifier of the newly created role.</summary>
    public Guid RoleId { get; }

    public Response(Guid roleId)
    {
      RoleId = Guard.Against.NullOrEmpty(roleId);
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response(RoleIds.Administrator);
  }
}
