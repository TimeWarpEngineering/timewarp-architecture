#region Purpose
// Endpoint-centric contract for opening one principal in the admin Principals area.
#endregion

#region Design
// Task 328: GET api/admin/principals/{PrincipalId} returns status, effective roles, and the
// profile. admin.principals.read gates the endpoint (same as ListPrincipals); the profile is
// gated inside on admin.profiles.read and is null without it, and also null when the principal
// has no profile row (agents; a human who never opened Settings). Opening a principal never
// creates a profile. A missing principal is 404. PrincipalId is the route segment, generated on
// the partial Query like GetFeedbackForAdmin's id. GetMockResponseFactory serves the SPA offline.
// AuthenticationSchemes (task 158): identity-session + mock-identity-session.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

using TimeWarp.Identity;

/// <summary>Open one principal with its roles and (permission-gated) profile.</summary>
[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.AdminPrincipalsRead,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class GetPrincipal
{
  [ApiRoute("api/admin/principals/{PrincipalId:guid}", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
      RuleFor(query => query.PrincipalId).NotEmpty();
    }
  }

  public sealed class Response : BaseResponse
  {
    public PrincipalDetailDto Principal { get; }

    public Response(PrincipalDetailDto principal)
    {
      Principal = Guard.Against.Null(principal);
    }
  }

  public sealed class PrincipalDetailDto : ICloneable
  {
    /// <summary>Immutable (get-only members), so a State clone can share the instance.</summary>
    public object Clone() => this;

    public PrincipalId PrincipalId { get; }
    public PrincipalKind Kind { get; }
    public TrustTier TrustTier { get; }
    public bool IsActive { get; }
    public bool IsQuarantined { get; }
    public List<Guid> RoleIds { get; }
    public PrincipalProfileDto? Profile { get; }

    public PrincipalDetailDto(
      PrincipalId principalId,
      PrincipalKind kind,
      TrustTier trustTier,
      bool isActive,
      bool isQuarantined,
      List<Guid> roleIds,
      PrincipalProfileDto? profile = null)
    {
      PrincipalId = principalId;
      Kind = kind;
      TrustTier = trustTier;
      IsActive = isActive;
      IsQuarantined = isQuarantined;
      RoleIds = roleIds ?? [];
      Profile = profile;
    }
  }

  public sealed class PrincipalProfileDto : ICloneable
  {
    /// <summary>Immutable (get-only members), so a State clone can share the instance.</summary>
    public object Clone() => this;

    public string DisplayName { get; }
    public string? Email { get; }
    public string Language { get; }
    public string Region { get; }
    public string Theme { get; }
    public bool Notifications { get; }

    public PrincipalProfileDto(
      string displayName,
      string? email,
      string language,
      string region,
      string theme,
      bool notifications)
    {
      DisplayName = displayName;
      Email = email;
      Language = language;
      Region = region;
      Theme = theme;
      Notifications = notifications;
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory() =>
    request => new Response(new PrincipalDetailDto(
      PrincipalId.From(
        request is Query { PrincipalId: var id } && id != Guid.Empty
          ? id
          : Guid.Parse("11111111-1111-1111-1111-111111111111")),
      PrincipalKind.Human,
      TrustTier.Keyed,
      isActive: true,
      isQuarantined: false,
      roleIds: [RoleIds.Member],
      profile: new PrincipalProfileDto("Ada Lovelace", "ada@example.com", "en-US", "US", "system", notifications: true)));
}
