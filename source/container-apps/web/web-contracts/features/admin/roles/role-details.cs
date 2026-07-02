#region Purpose
// Shared editable-role shape and validation rules for the Roles feature contracts.
#endregion

#region Design
// Validating against the interface lets CreateRole.Command, UpdateRole.Command, and
// GetRole.Response share one rule set via SetValidator, keeping client-side form validation
// identical across create and edit screens. RoleId is deliberately excluded: each contract
// sources it differently (route parameter vs. body vs. server-assigned response value).
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

public interface IRoleDetails
{
  public string Name { get; set; }
  public string Description { get; set; }
}

public sealed class RoleDetailsValidator : AbstractValidator<IRoleDetails>
{
  public RoleDetailsValidator()
  {
    RuleFor(r => r.Name).NotEmpty();
    RuleFor(r => r.Description).NotEmpty();
  }
}
