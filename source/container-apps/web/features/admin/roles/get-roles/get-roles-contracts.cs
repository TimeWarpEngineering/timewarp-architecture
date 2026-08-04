#region Purpose
// Endpoint-centric contract for listing roles with OData-style paging/sorting.
#endregion

#region Design
// Uses the [AuthApiRequest] and [OpenDataQueryParameters] attributes (not the interfaces)
// so the source generator emits both the auth members and the paging members onto the
// partial Query; GetRouteWithQueryString then merges the two generated parameter sets into
// one query string. RoleDto is a flat read model separate from IRoleDetails because list
// rows are display-only, and Response derives from ListResponse to carry TotalCount for
// paging. GetMockResponseFactory serves the SPA's MockWebApiService offline.
// [EndpointAuthorize] (task 110): [AuthApiRequest] on the Query is a client-facing/mock-mode
// identity signal only — TWA0014 would flag pairing it with [EndpointAllowAnonymous] as a
// contradiction, so this attribute is what actually gates the generated endpoint. See CreateRole's
// Design region for the policy/scope-boundary rationale (same policy, same "no admin/role model
// yet" boundary).
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

/// <summary>
/// Get a list of roles for display only.
/// </summary>
[ApiEndpoint]
[EndpointAuthorize(Policy = "identity-session-authenticated")] // matches IdentitySessionDefaults.AuthenticatedPolicy
public static partial class GetRoles
{

  [ApiRoute("api/Roles", HttpVerb.Get)]
  [OpenDataQueryParameters]
  [AuthApiRequest]
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

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    RoleDto[] items =
    [
      new(RoleIds.Member, nameof(RoleIds.Member), "Default human principal after passkey login."),
      new(RoleIds.Operator, nameof(RoleIds.Operator), "Marketplace and job oversight."),
      new(RoleIds.Administrator, nameof(RoleIds.Administrator), "Tenant admin: principals, roles, settings."),
      new(RoleIds.Developer, nameof(RoleIds.Developer), "Template dogfood: demos and diagnostics."),
    ];

    return _ => new Response(totalCount: items.Length, items);
  }
}
