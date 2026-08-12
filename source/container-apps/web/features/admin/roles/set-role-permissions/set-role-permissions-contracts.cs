#region Purpose
// Endpoint-centric contract for replacing a role's permission membership (role as bundle).
#endregion

#region Design
// Task 182-004: PUT api/Roles/{RoleId}/permissions — full replace of IRolePermissionStore grants
// for one product role. RoleId from route (source-generated on partial Command). Validator
// requires every PermissionId ∈ PermissionIds.All (catalog only — no free-form strings).
// [EndpointAuthorize] admin.roles.manage; AuthenticationSchemes identity-session +
// mock-identity-session (same as Create/Update/Delete). Handler enforces protected-core on
// Administrator (RolePermissionSeed.AdminPermissions cannot be stripped) — store stays dumb.
// Response echoes stored permission ids after write.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

/// <summary>Replace stored permission ids for a role.</summary>
[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.AdminRolesManage,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class SetRolePermissions
{
  [ApiRoute("api/Roles/{RoleId:guid}/permissions", HttpVerb.Put)]
  public sealed partial class Command : IAuthApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }
    public List<string> PermissionIds { get; set; } = [];
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x).SetValidator(new AuthApiRequestValidator());
      RuleFor(x => x.RoleId).NotEmpty();
      RuleFor(x => x.PermissionIds).NotNull();
      // Fully qualify product PermissionIds — Command.PermissionIds property would otherwise shadow.
      RuleForEach(x => x.PermissionIds)
        .Must(static id => global::TimeWarp.Architecture.Features.PermissionIds.All.Contains(id))
        .WithMessage("Each PermissionId must be a product permission (PermissionIds.All).");
    }
  }

  public sealed class Response
  {
    public List<string> PermissionIds { get; }

    public Response(List<string> permissionIds)
    {
      PermissionIds = permissionIds ?? [];
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response(
    [
      PermissionIds.AdminAccess,
      PermissionIds.AdminRolesRead,
      PermissionIds.AdminRolesManage,
      PermissionIds.AdminPrincipalsRead,
      PermissionIds.AdminPrincipalsManage,
      PermissionIds.ProfileRead,
      PermissionIds.SettingsRead,
    ]);
  }
}
