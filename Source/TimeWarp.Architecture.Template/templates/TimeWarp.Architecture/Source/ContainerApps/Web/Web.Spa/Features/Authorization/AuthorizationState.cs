namespace TimeWarp.Architecture.Features.Authorization;

[StateAccessMixin]
internal sealed partial class AuthorizationState : State<AuthorizationState>
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
