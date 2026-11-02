#region Purpose
// Endpoint-centric contract for opening one principal in the admin Principals area.
#endregion

#region Design
// Task 328: GET api/admin/principals/{PrincipalId} returns status, effective roles, and the
// profile. admin.principals.read gates the endpoint (same as ListPrincipals); the profile is
// gated inside on admin.profiles.read and is null without it, and also null when the principal
// has no profile row (agents; a human who never opened Settings). Opening a principal never
// creates a profile. A missing principal is 404. PrincipalId is the route segment, generated on
// the partial Query like GetFeedbackForAdmin's id. AgentScopeGrants are the principal's own
// grants. ApprovedAgentScopes is the effective set (identity:read, role scopes, and those
// grants), each source marked default, role, or principal (task 466). GetMockResponseFactory
// serves the SPA offline.
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
  public sealed partial class Query : IApiRequest, IQuery<OneOf<Response, SharedProblemDetails>>;

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

    /// <summary>Agent scopes granted on this principal, apart from its roles.</summary>
    public List<string> AgentScopeGrants { get; }

    /// <summary>Scopes the principal may request at issuance, with each source.</summary>
    public List<ApprovedAgentScopeDto> ApprovedAgentScopes { get; }

    public PrincipalDetailDto(
      PrincipalId principalId,
      PrincipalKind kind,
      TrustTier trustTier,
      bool isActive,
      bool isQuarantined,
      List<Guid> roleIds,
      PrincipalProfileDto? profile = null,
      List<string>? agentScopeGrants = null,
      List<ApprovedAgentScopeDto>? approvedAgentScopes = null)
    {
      PrincipalId = principalId;
      Kind = kind;
      TrustTier = trustTier;
      IsActive = isActive;
      IsQuarantined = isQuarantined;
      RoleIds = roleIds ?? [];
      Profile = profile;
      AgentScopeGrants = agentScopeGrants ?? [];
      ApprovedAgentScopes = approvedAgentScopes ?? [];
    }
  }

  /// <summary>Wire values for <see cref="ApprovedAgentScopeSourceDto.Kind"/>.</summary>
  public static class AgentScopeSourceKinds
  {
    public const string Default = "default";
    public const string Role = "role";
    public const string Principal = "principal";
  }

  public sealed class ApprovedAgentScopeSourceDto
  {
    public string Kind { get; }
    public Guid? RoleId { get; }

    public ApprovedAgentScopeSourceDto(string kind, Guid? roleId = null)
    {
      Kind = kind;
      RoleId = roleId;
    }
  }

  public sealed class ApprovedAgentScopeDto
  {
    public string Scope { get; }
    public List<ApprovedAgentScopeSourceDto> Sources { get; }

    public ApprovedAgentScopeDto(string scope, List<ApprovedAgentScopeSourceDto> sources)
    {
      Scope = scope;
      Sources = sources ?? [];
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
      profile: new PrincipalProfileDto("Ada Lovelace", "ada@example.com", "en-US", "US", "system", notifications: true),
      agentScopeGrants: [],
      approvedAgentScopes:
      [
        new ApprovedAgentScopeDto(
          AgentScopes.IdentityRead,
          [new ApprovedAgentScopeSourceDto(AgentScopeSourceKinds.Default)])
      ]));
}
