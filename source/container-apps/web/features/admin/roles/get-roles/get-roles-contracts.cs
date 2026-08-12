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
// [EndpointAuthorize] (task 182-002): admin.roles.read via PermissionIds — read half of the
// roles split (manage is Create/Update/Delete). Server PermissionRequirementHandler evaluates
// via IPermissionEvaluator; SPA pages use PermissionIds.AdminRolesRead (182-003 claim policies).
// [AuthApiRequest] on the Query remains a client-facing/mock-mode identity signal only.
// AuthenticationSchemes (task 158): identity-session + mock-identity-session so the generated
// FastEndpoint's AuthSchemes(...) invokes the mock handler under closed-box ingress tests —
// without this, Policies(...) alone never runs mock-identity-session and non-admin mock
// principals fall through as 401 instead of 403. See AuthenticationSchemeNames' Design region.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

/// <summary>
/// Get a list of roles for display only.
/// </summary>
[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.AdminRolesRead,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
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
