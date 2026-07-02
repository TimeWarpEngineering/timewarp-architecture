#region Purpose
// Client-side cache of the signed-in user's module and role grants, the source for authorization decisions.
#endregion

#region Design
// Grants are opaque Guids, not names: the server owns their meaning; the client only matches
// them (ModuleRequirementHandler, role claims in the claims principal factory).
// Cacheable (BaseCacheableState, 30s default) so repeated policy evaluations do not refetch.
// Mutable lists stay private so nested action handlers are the only writers; consumers get
// read-only views.
#endregion

namespace TimeWarp.Architecture.Features.Authorization;

[StateAccess]
public sealed partial class AuthorizationState : BaseCacheableState<AuthorizationState>
{
  private List<Guid>? ModulesList { get; set; }
  private List<Guid>? RolesList { get; set; }

  // ReSharper disable once ReturnTypeCanBeEnumerable.Global
  public IReadOnlyList<Guid>? Modules => ModulesList?.AsReadOnly();
  public IReadOnlyList<Guid>? Roles => RolesList?.AsReadOnly();

  public override void Initialize()
  {
    ModulesList = null;
    RolesList = null;
  }
}
