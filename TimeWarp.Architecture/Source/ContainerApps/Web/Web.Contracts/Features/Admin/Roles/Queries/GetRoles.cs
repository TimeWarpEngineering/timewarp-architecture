namespace TimeWarp.Architecture.Features.Admin.Roles;

using Authorization;

/// <summary>
/// Get a list of roles for display only.
/// </summary>
public static partial class GetRoles
{

  [RouteMixin("api/Roles", HttpVerb.Get)]
  [IOpenDataQueryParametersMixin]
  [IAuthApiRequestMixin]
  public sealed partial class Query : IQueryStringRouteProvider, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public string GetRouteWithQueryString()
    {
      var collection = new NameValueCollection
      {
        GetAuthQueryParameters(),
        GetOpenDataQueryParameters()
      };
      return $"{GetRoute()}?{this.GetQueryString(collection)}";
    }
  }

  public sealed class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
      RuleFor(x => x).SetValidator(new AuthApiRequestValidator());
    }
  }

  public sealed class Response : ListResponse<RoleDto>
  {
    public Response(int totalCount, RoleDto[] items) : base(totalCount, items) {}
  }

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

  public static MockResponseFactory<Response> GetMockResponseFactory(IApiRequest request)
  {
    RoleDto[] items =
    [
      new
      (
        roleId: RoleIds.Administrator,
        name: nameof(RoleIds.Administrator),
        description: "The Administrator role is for administrators. And has access to all modules."
      ),
      new
      (
        roleId: RoleIds.Accountant,
        name: nameof(RoleIds.Accountant),
        description: "The Accountant role is for accountants. And has access to the accounting module."
      ),
    ];

    return _ => new Response(totalCount: items.Length, items);
  }
}
