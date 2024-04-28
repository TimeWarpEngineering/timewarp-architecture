namespace TimeWarp.Architecture.Features.Admin.Roles;

public static partial class GetRole
{
  [RouteMixin("api/Roles/{RoleId:min(1)}", HttpVerb.Get)]
  public sealed partial class Query : IAuthApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }
  }

  public sealed class Validator : AbstractValidator<Query>;

  public sealed class Response
  (
    Guid roleId,
    string name,
    string description
  ) : IRoleDetails
  {
    public Guid RoleId { get; init; } = roleId != Guid.Empty ? roleId : throw new ArgumentException("RoleId cannot be Empty", nameof(roleId));
    public string Name { get; set; } = !string.IsNullOrEmpty(name) ? name : throw new ArgumentException("Name cannot be Empty", nameof(name));
    public string Description { get; set; } = !string.IsNullOrEmpty(description) ? description : throw new ArgumentException("Description cannot be Empty", nameof(description));
  }
}
