namespace TimeWarp.Architecture.Features.Admin.Roles;

public static partial class GetRoles
{

  [RouteMixin("api/Roles", HttpVerb.Get)]
  public sealed partial class Query : IAuthApiRequest, IOpenDataQueryParameters, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }
    public int? Top { get; set; }
    public int? Skip { get; set; }
    public string? Filter { get; set; }
    public string? OrderBy { get; set; }
    public bool ReturnTotalCount { get; set; }
  }

  public sealed class Response
  (
    int totalCount,
    RoleDto[] items
  ) : ListResponse<RoleDto>(totalCount, items);

  public sealed class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
      RuleFor(x => x).SetValidator(new AuthApiRequestValidator());
    }
  }

  public sealed class RoleDto
  (
    Guid roleId,
    string name,
    string description
  )
  {
    public Guid RoleId { get; init; } = roleId != Guid.Empty ? roleId : throw new ArgumentException("RoleId cannot be Empty", nameof(roleId));
    public string Name { get; init; } = !string.IsNullOrEmpty(name) ? name : throw new ArgumentException("Name cannot be Empty", nameof(name));
    public string Description { get; init; } = !string.IsNullOrEmpty(description) ? description : throw new ArgumentException("Description cannot be Empty", nameof(description));
  }
}
