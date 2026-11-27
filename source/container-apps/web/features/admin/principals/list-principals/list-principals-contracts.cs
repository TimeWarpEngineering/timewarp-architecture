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
// Task 345: [ReferenceableResource] makes this the "principal" @ type in Ask, labelled by
// DisplayName (the id when the caller cannot read profiles). Typing a search term needs
// admin.profiles.read like any other search here.
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
[ReferenceableResource("principal", "Principals", IdProperty = nameof(ListPrincipals.PrincipalSummaryDto.PrincipalId), LabelProperty = nameof(ListPrincipals.PrincipalSummaryDto.DisplayName))]
public static partial class ListPrincipals
{
  [ApiRoute("api/admin/principals", HttpVerb.Get)]
  [Operation("principals.list", Description = "List principals, optionally filtered by a search term and sorted.", Group = "admin.principals")]
  public sealed partial class Query : IAuthApiRequest, IQueryStringRouteProvider, IQuery<OneOf<Response, SharedProblemDetails>>
  {
    /// <summary>Text matched against principal names and emails.</summary>
    public string? Search { get; set; }
    /// <summary>Column to sort by.</summary>
    public PrincipalListSortColumn SortBy { get; set; } = PrincipalListSortColumn.CreatedAt;
    /// <summary>Whether to sort in descending order.</summary>
    public bool SortDescending { get; set; }

    public string GetRouteWithQueryString()
    {
      var collection = new NameValueCollection
      {
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
      RuleFor(x => x.SortBy).IsInEnum();
      // 254 = Profile.MaxEmailLength (contracts cannot reference the domain).
      RuleFor(x => x.Search).MaximumLength(254);
    }
  }

  public sealed class Response : ListResponse<PrincipalSummaryDto>
  {
    public Response(int totalCount, PrincipalSummaryDto[] items) : base(totalCount, items) { }
  }

  public sealed class PrincipalSummaryDto : ICloneable
  {
    /// <summary>Immutable (get-only members), so a State clone can share the instance.</summary>
    public object Clone() => this;

    /// <summary>Id of the principal.</summary>
    public PrincipalId PrincipalId { get; }
    /// <summary>Whether the principal is a human or an agent.</summary>
    public PrincipalKind Kind { get; }
    /// <summary>How the principal proved its identity.</summary>
    public TrustTier TrustTier { get; }
    /// <summary>Whether the principal can sign in.</summary>
    public bool IsActive { get; }
    /// <summary>Whether the principal is held back from acting.</summary>
    public bool IsQuarantined { get; }
    /// <summary>Ids of the roles assigned to the principal.</summary>
    public List<Guid> RoleIds { get; }
    /// <summary>Name shown for the principal.</summary>
    public string? DisplayName { get; }
    /// <summary>Email address, when known.</summary>
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
