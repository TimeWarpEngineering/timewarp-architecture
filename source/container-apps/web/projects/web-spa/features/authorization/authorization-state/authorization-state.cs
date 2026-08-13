#region Purpose
// Client-side cache of the signed-in user's role and permission grants (Entra / mock path).
#endregion

#region Design
// Grants are RoleIds Guids + PermissionIds strings (task 182-003 deleted Modules /
// ModuleRequirement). The identity-session path projects permissions from GetCurrentSession
// into claims without this cache; Entra AccountClaimsPrincipalFactoryWithRoles still fetches
// via GetCurrentUser and stores Roles + Permissions here for claim projection.
// Cacheable (BaseCacheableState, 30s default) so repeated fetches do not thrash.
// Mutable lists stay private so nested action handlers are the only writers; consumers get
// read-only views.
#endregion

namespace TimeWarp.Architecture.Features.Authorization;

[StateAccess]
public sealed partial class AuthorizationState : BaseCacheableState<AuthorizationState>
{
  private List<Guid>? RolesList { get; set; }
  private List<string>? PermissionsList { get; set; }

  public IReadOnlyList<Guid>? Roles => RolesList?.AsReadOnly();
  public IReadOnlyList<string>? Permissions => PermissionsList?.AsReadOnly();

  public override void Initialize()
  {
    RolesList = null;
    PermissionsList = null;
  }
}
