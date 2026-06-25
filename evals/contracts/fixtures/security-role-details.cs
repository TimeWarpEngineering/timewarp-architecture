// Eval fixture: bindable interface + shared validator (incomplete — missing Command/Query).
namespace MyApp.Features.Admin.SecurityRoles;

public interface ISecurityRoleDetails
{
  public string Name { get; set; }
  public string? Description { get; set; }
  public string Code { get; set; }
}

public sealed class SecurityRoleDetailsValidator : AbstractValidator<ISecurityRoleDetails>
{
  public SecurityRoleDetailsValidator()
  {
    RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
    RuleFor(x => x.Description).MaximumLength(255).When(x => x.Description != null);
    RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
  }
}