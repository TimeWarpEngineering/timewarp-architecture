namespace TimeWarp.Architecture.Features.Admin.Roles;

/// <summary>
/// Get a list of roles for display only.
/// </summary>
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

  public sealed class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
      RuleFor(x => x).SetValidator(new AuthApiRequestValidator());
    }
  }

  public sealed class Response
  (
    int totalCount,
    RoleDto[] items
  ) : ListResponse<RoleDto>(totalCount, items);

  public sealed class RoleDto
  {
    public Guid RoleId { get; }
    public string Name { get; }
    public string Description { get; }

    public RoleDto
    (
      Guid roleId,
      string name,
      string description
    )
    {
      RoleId = Guard.Against.NullOrEmpty(roleId);
      Name = Guard.Against.NullOrEmpty(name);
      Description = Guard.Against.NullOrEmpty(description);
    }
  }
}
