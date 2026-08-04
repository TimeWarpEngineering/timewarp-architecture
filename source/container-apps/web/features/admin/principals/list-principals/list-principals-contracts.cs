#region Purpose
// Endpoint-centric contract for listing principals with effective roles for the admin UI.
#endregion

#region Design
// Task 147-004: admin Principals list (D9) — flat PrincipalSummaryDto rows, no detail page.
// RoleIds on each row are *effective* roles (IEffectiveRolesResolver), not raw store rows, so the
// multi-select UI shows what RequireRole will see. [EndpointAuthorize] uses CanViewPrincipalsPage
// (Administrator). GetMockResponseFactory serves SPA MockWebApiService offline.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

using TimeWarp.Identity;

/// <summary>List principals for admin role assignment.</summary>
[ApiEndpoint]
[EndpointAuthorize(Policy = AuthorizationPolicyNames.CanViewPrincipalsPage)]
public static partial class ListPrincipals
{
  [ApiRoute("api/admin/principals", HttpVerb.Get)]
  [AuthApiRequest]
  public sealed partial class Query : IQueryStringRouteProvider, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public string GetRouteWithQueryString()
    {
      var collection = new NameValueCollection
      {
        GetAuthQueryParameters()
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

  public sealed class Response : ListResponse<PrincipalSummaryDto>
  {
    public Response(int totalCount, PrincipalSummaryDto[] items) : base(totalCount, items) {}
  }

  public sealed class PrincipalSummaryDto
  {
    public PrincipalId PrincipalId { get; }
    public PrincipalKind Kind { get; }
    public TrustTier TrustTier { get; }
    public bool IsActive { get; }
    public bool IsQuarantined { get; }
    public List<Guid> RoleIds { get; }

    public PrincipalSummaryDto(
      PrincipalId principalId,
      PrincipalKind kind,
      TrustTier trustTier,
      bool isActive,
      bool isQuarantined,
      List<Guid> roleIds)
    {
      PrincipalId = principalId;
      Kind = kind;
      TrustTier = trustTier;
      IsActive = isActive;
      IsQuarantined = isQuarantined;
      RoleIds = roleIds ?? [];
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
        roleIds: [RoleIds.Member, RoleIds.Administrator]),
      new(
        PrincipalId.From(Guid.Parse("22222222-2222-2222-2222-222222222222")),
        PrincipalKind.Human,
        TrustTier.Keyed,
        isActive: true,
        isQuarantined: false,
        roleIds: [RoleIds.Member]),
    ];

    return _ => new Response(totalCount: items.Length, items);
  }
}
