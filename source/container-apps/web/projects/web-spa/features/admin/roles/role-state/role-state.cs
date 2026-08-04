#region Purpose
// SPA state for admin roles: list from GetRoles plus last-created id for the New Role demo.
#endregion

#region Design
// Roles null = not loaded (loading UI); empty array = loaded with zero rows.
// LastCreatedRoleId remains for create-round-trip demos after RoleForm submit.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

using static GetRoles;

[StateAccess]
public sealed partial class RoleState : State<RoleState>
{
  private List<RoleDto>? RolesList { get; set; }

  public IReadOnlyList<RoleDto>? Roles => RolesList?.AsReadOnly();

  // Id of the most recently created role (demo: lets a page confirm the create round-trip).
  public Guid? LastCreatedRoleId { get; private set; }

  public override void Initialize()
  {
    RolesList = null;
    LastCreatedRoleId = null;
  }
}
