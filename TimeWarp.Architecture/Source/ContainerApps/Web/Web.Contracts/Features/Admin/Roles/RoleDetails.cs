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
