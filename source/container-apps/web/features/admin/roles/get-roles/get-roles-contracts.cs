#region Purpose
// Endpoint-centric contract for listing roles with OData-style paging/sorting.
#endregion

#region Design
// Uses the [AuthApiRequest] and [OpenDataQueryParameters] attributes (not the interfaces)
// so the source generator emits both the auth members and the paging members onto the
// partial Query; GetRouteWithQueryString then merges the two generated parameter sets into
// one query string. RoleDto is a flat read model separate from IRoleDetails because list
// rows are display-only, and Response derives from ListResponse to carry TotalCount for
// paging. PermissionIds on RoleDto (task 182-004 / 206) feed list summary chips and
// RoleDetailPage drafts — fine for the small product role catalog (RoleIds.All + demo creates).
// GetMockResponseFactory serves the SPA's MockWebApiService offline.
// [EndpointAuthorize] (task 182-002): admin.roles.read via PermissionIds — read half of the
// roles split (manage is Create/Update/Delete/SetRolePermissions). Server
// PermissionRequirementHandler evaluates via IPermissionEvaluator; SPA pages use
// PermissionIds.AdminRolesRead (182-003 claim policies).
// [AuthApiRequest] on the Query remains a client-facing/mock-mode identity signal only.
// AuthenticationSchemes (task 158 / 161): identity-session + mock-identity-session so the
// generated FastEndpoint's AuthSchemes(...) invokes the mock handler under closed-box ingress
// tests. PermissionIds policies have no AddAuthenticationSchemes; Policies(...) alone would
// authenticate only the default identity-session scheme and non-admin mock principals would
// fall through as 401 instead of 403. See AuthenticationSchemeNames' Design region.
// Task 345: Search keeps roles whose name contains the term (case-insensitive), and
// [ReferenceableResource] makes this the "role" @ type in Ask, labelled by name.
// [Operation] names it roles.list for every transport; IQuery is its kind (tw-web-api-contracts).
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
[ReferenceableResource("role", "Roles", IdProperty = nameof(GetRoles.RoleDto.RoleId), LabelProperty = nameof(GetRoles.RoleDto.Name))]
public static partial class GetRoles
{
  public const int MaxSearchLength = 200;

  [ApiRoute("api/Roles", HttpVerb.Get)]
  [Operation("roles.list", Description = "List roles, optionally filtered by a name search.", Group = "admin.roles")]
  [OpenDataQueryParameters]
  [AuthApiRequest]
  public sealed partial class Query : IQueryStringRouteProvider, IQuery<OneOf<Response, SharedProblemDetails>>
  {
    public string? Search { get; set; }

    public string GetRouteWithQueryString()
    {
      var collection = new NameValueCollection
      {
        GetAuthQueryParameters(),
        GetOpenDataQueryParameters(),
        { nameof(Search), string.IsNullOrWhiteSpace(Search) ? null : Search.Trim() },
      };
      return $"{GetRoute()}?{this.GetQueryString(collection)}";
    }
  }

  public sealed class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
      RuleFor(x => x).SetValidator(new AuthApiRequestValidator());
      RuleFor(x => x.Search).MaximumLength(MaxSearchLength);
    }
  }

  public sealed class Response : ListResponse<RoleDto>
  {
    public Response(int totalCount, RoleDto[] items) : base(totalCount, items) { }
  }

  public sealed class RoleDto : ICloneable
  {
    /// <summary>Immutable (get-only members), so a State clone can share the instance.</summary>
    public object Clone() => this;

    public Guid RoleId { get; }
    public string Name { get; }
    public string Description { get; }

    /// <summary>Stored permission ids for this role (empty when none granted).</summary>
    public List<string> PermissionIds { get; }

    public RoleDto
    (
      Guid roleId,
      string name,
      string description,
      List<string>? permissionIds = null
    )
    {
      RoleId = Guard.Against.NullOrEmpty(roleId);
      Name = Guard.Against.NullOrEmpty(name);
      Description = Guard.Against.NullOrEmpty(description);
      PermissionIds = permissionIds ?? [];
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    RoleDto[] items =
    [
      new(
        RoleIds.Member,
        nameof(RoleIds.Member),
        "Default human principal after passkey login.",
        [PermissionIds.ProfileRead, PermissionIds.SettingsRead]),
      new(
        RoleIds.Operator,
        nameof(RoleIds.Operator),
        "Marketplace and job oversight.",
        [PermissionIds.ProfileRead, PermissionIds.SettingsRead]),
      new(
        RoleIds.Administrator,
        nameof(RoleIds.Administrator),
        "Tenant admin: principals, roles, settings.",
        [
          PermissionIds.AdminAccess,
          PermissionIds.AdminRolesRead,
          PermissionIds.AdminRolesManage,
          PermissionIds.AdminPrincipalsRead,
          PermissionIds.AdminPrincipalsManage,
          PermissionIds.AdminProfilesRead,
          PermissionIds.AdminFeedbackRead,
          PermissionIds.AdminFeedbackManage,
          PermissionIds.ProfileRead,
          PermissionIds.SettingsRead,
        ]),
      new(
        RoleIds.Developer,
        nameof(RoleIds.Developer),
        "Template dogfood: demos and diagnostics.",
        [
          PermissionIds.DeveloperAccess,
          PermissionIds.DeveloperClaimsRead,
          PermissionIds.ProfileRead,
          PermissionIds.SettingsRead,
        ]),
    ];

    return request =>
    {
      string? search = (request as Query)?.Search?.Trim();
      RoleDto[] matches = string.IsNullOrEmpty(search)
        ? items
        : [.. items.Where(role => role.Name.Contains(search, StringComparison.OrdinalIgnoreCase))];
      return new Response(totalCount: matches.Length, matches);
    };
  }
}
