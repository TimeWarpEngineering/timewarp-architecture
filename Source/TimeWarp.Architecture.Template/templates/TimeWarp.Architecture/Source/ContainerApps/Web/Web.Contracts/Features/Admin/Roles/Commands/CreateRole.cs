namespace TimeWarp.Architecture.Features.Admin.Roles;

public static partial class CreateRole
{
  [RouteMixin("api/Roles", HttpVerb.Post)]
  public sealed partial class Command : IAuthApiRequest, IRoleDetails, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
  }

  public sealed class Response(Guid roleId)
  {
    public Guid RoleId { get; init; } = roleId != Guid.Empty ? roleId : throw new ArgumentException("RoleId cannot be Empty", nameof(roleId));
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x).SetValidator(new RoleDetailsValidator());
      RuleFor(x => x).SetValidator(new AuthApiRequestValidator());
    }
  }
}
