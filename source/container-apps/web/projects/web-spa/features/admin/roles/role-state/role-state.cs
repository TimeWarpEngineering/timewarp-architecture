#region Purpose
// Tracks the id of the most recently created role so the roles demo page can confirm the create round-trip.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

[StateAccess]
public sealed partial class RoleState : State<RoleState>
{
  // Id of the most recently created role (demo: lets a page confirm the create round-trip).
  public Guid? LastCreatedRoleId { get; private set; }

  public override void Initialize() => LastCreatedRoleId = null;
}
