#region Purpose
// Endpoint-centric contract for listing principals with effective roles for the admin UI.
#endregion

#region Design
// Task 147-004: admin Principals list (D9) — flat PrincipalSummaryDto rows, no detail page.
// RoleIds on each row are *effective* roles (IEffectiveRolesResolver), not raw store rows.
// [EndpointAuthorize] (task 182-002): admin.principals.read (PermissionIds); SetPrincipalRoles
// uses admin.principals.manage. GetMockResponseFactory serves SPA MockWebApiService offline.
// AuthenticationSchemes (task 158): identity-session + mock-identity-session — see
// AuthenticationSchemeNames' Design region.
// Task 328: rows gain DisplayName and Email from the profile, populated only when the caller
// holds admin.profiles.read (null otherwise; agents and principals without a profile are null).
// Search (contains, case-insensitive, on display name or email), SortBy, and SortDescending
// order the whole list server-side; search and profile-field sorts need admin.profiles.read
// (403 without it). Search MaximumLength 254 is Profile.MaxEmailLength; contracts cannot
// reference the domain, so the literal is duplicated here. Defaults are CreatedAt ascending,
// the store order.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

using TimeWarp.Identity;

/// <summary>List principals for admin role assignment.</summary>
[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.AdminPrincipalsRead,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class ListPrincipals
{
  [ApiRoute("api/admin/principals", HttpVerb.Get)]
  [AuthApiRequest]
  public sealed partial class Query : IQueryStringRouteProvider, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public string? Search { get; set; }
    public PrincipalListSortColumn SortBy { get; set; } = PrincipalListSortColumn.CreatedAt;
    public bool SortDescending { get; set; }

    public string GetRouteWithQueryString()
    {
      var collection = new NameValueCollection
      {
        GetAuthQueryParameters(),
        { nameof(Search), string.IsNullOrWhiteSpace(Search) ? null : Search.Trim() },
        { nameof(SortBy), SortBy.ToString() },
        { nameof(SortDescending), SortDescending ? "true" : "false" },
      };
      return $"{GetRoute()}?{this.GetQueryString(collection)}";
    }
  }

  public sealed class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
      RuleFor(x => x).SetValidator(new AuthApiRequestValidator());
      RuleFor(x => x.SortBy).IsInEnum();
      // 254 = Profile.MaxEmailLength (contracts cannot reference the domain).
      RuleFor(x => x.Search).MaximumLength(254);
    }
  }

  public sealed class Response : ListResponse<PrincipalSummaryDto>
  {
    public Response(int totalCount, PrincipalSummaryDto[] items) : base(totalCount, items) { }
  }

  public sealed class PrincipalSummaryDto
  {
    public PrincipalId PrincipalId { get; }
    public PrincipalKind Kind { get; }
    public TrustTier TrustTier { get; }
    public bool IsActive { get; }
    public bool IsQuarantined { get; }
    public List<Guid> RoleIds { get; }
    public string? DisplayName { get; }
    public string? Email { get; }

    public PrincipalSummaryDto(
      PrincipalId principalId,
      PrincipalKind kind,
      TrustTier trustTier,
      bool isActive,
      bool isQuarantined,
      List<Guid> roleIds,
      string? displayName = null,
      string? email = null)
    {
      PrincipalId = principalId;
      Kind = kind;
      TrustTier = trustTier;
      IsActive = isActive;
      IsQuarantined = isQuarantined;
      RoleIds = roleIds ?? [];
      DisplayName = displayName;
      Email = email;
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    PrincipalSummaryDto[] items =
    [
      new(
        PrincipalId.From(Guid.Parse("11111111-1111-1111-1111-111111111111")),
        PrincipalKind.Human,
        TrustTier.Keyed,
        isActive: true,
        isQuarantined: false,
        roleIds: [RoleIds.Member, RoleIds.Administrator],
        displayName: "Ada Lovelace",
        email: "ada@example.com"),
      new(
        PrincipalId.From(Guid.Parse("22222222-2222-2222-2222-222222222222")),
        PrincipalKind.Human,
        TrustTier.Keyed,
        isActive: true,
        isQuarantined: false,
        roleIds: [RoleIds.Member],
        displayName: "Grace Hopper",
        email: "grace@example.com"),
    ];

    return _ => new Response(totalCount: items.Length, items);
  }
}
