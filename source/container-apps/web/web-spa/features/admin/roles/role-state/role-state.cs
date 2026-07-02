namespace TimeWarp.Architecture.Features.Admin.Roles;

[StateAccess]
public sealed partial class RoleState : State<RoleState>
{
  // Id of the most recently created role (demo: lets a page confirm the create round-trip).
  public Guid? LastCreatedRoleId { get; private set; }

  public override void Initialize() => LastCreatedRoleId = null;
}
