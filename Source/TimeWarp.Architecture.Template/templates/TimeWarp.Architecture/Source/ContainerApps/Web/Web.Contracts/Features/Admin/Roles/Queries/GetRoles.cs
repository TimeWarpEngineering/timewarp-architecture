namespace TimeWarp.Architecture.Features.Admin.Roles;

/// <summary>
/// Get a list of roles for display only.
/// </summary>
public static partial class GetRoles
{

  [RouteMixin("api/Roles", HttpVerb.Get)]
  [IOpenDataQueryParametersMixin]
  [IAuthApiRequestMixin]
  public sealed partial class Query : IRequest<OneOf<Response, SharedProblemDetails>>;

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
