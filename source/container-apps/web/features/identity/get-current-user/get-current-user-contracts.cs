#region Purpose
// Client-only contract for the signed-in user's role and permission grants (SPA mock / Entra demos).
#endregion

#region Design
// Not the identity "who am I" read — that is GetCurrentSession (cookie session / PrincipalId +
// server-expanded Permissions). This shape is application grants for SPA mock demos and the
// Entra AccountClaimsPrincipalFactoryWithRoles path (projects Role + permission claims).
// Roles are Guids from RoleIds; Permissions are PermissionIds strings (task 182-003 — Modules /
// ModuleIds deleted). The mock factory returns per-user responses keyed by MockUserIds; unknown
// users get full access because the mock optimizes for demo friction, not security.
// [ClientOnlyContract]: no server endpoint and no YARP ingress prefix (task 107/ingress tests).
// Rehomed under Features.Identity (task 104-021) so web/features no longer carries a near-empty
// authentication/ peer of identity/.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

public static partial class GetCurrentUser
{
  [ApiRoute(RouteTemplate: "api/GetCurrentUser", HttpVerb.Get)]
  [ClientOnlyContract("Served by SPA mock mode; no server grants endpoint in the template.")]
  public sealed partial class Query : IAuthApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }
  }

  public sealed class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
      RuleFor(x => x.UserId).NotEmpty().NotEqual(Guid.Empty);
    }
  }

  public sealed class Response
  {
    public Response
    (
      List<Guid> roles,
      List<string> permissions
    )
    {
      Roles = roles;
      Permissions = permissions;
    }

    /// <summary>
    /// Roles to which the current user belongs (from <see cref="RoleIds"/>).
    /// </summary>
    public List<Guid> Roles { get; init; }

    /// <summary>
    /// Permission ids the current user holds (from <see cref="PermissionIds"/>).
    /// SPA projects these as <see cref="PermissionIds.ClaimType"/> claims.
    /// </summary>
    public List<string> Permissions { get; init; }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return CreateMockResponse;
  }

  private static Response CreateMockResponse(IApiRequest request)
  {
    var query = (Query)request;

    var responseCreators = new Dictionary<Guid, Func<Response>>
    {
      { MockUserIds.SystemAdmin, CreateMockResponseForAdministrator },
      { MockUserIds.Developer, CreateMockResponseForDeveloper },
    };

    Response response =
      responseCreators.TryGetValue
      (
        query.UserId,
        out Func<Response>? responseCreator
      ) ? responseCreator() : CreateMockResponseForUnknown();

    return response;
  }

  private static Response CreateMockResponseForUnknown()
  {
    return new Response
    (
      roles:
      [
        RoleIds.Member,
        RoleIds.Administrator,
        RoleIds.Developer
      ],
      permissions: [.. PermissionIds.All]
    );
  }

  private static Response CreateMockResponseForAdministrator()
  {
    return new Response
    (
      roles:
      [
        RoleIds.Member,
        RoleIds.Administrator,
        RoleIds.Developer
      ],
      permissions: [.. PermissionIds.All]
    );
  }

  private static Response CreateMockResponseForDeveloper()
  {
    return new Response
    (
      roles: [RoleIds.Member, RoleIds.Developer],
      permissions:
      [
        PermissionIds.DeveloperAccess,
        PermissionIds.DeveloperClaimsRead,
        PermissionIds.ProfileRead,
        PermissionIds.SettingsRead,
      ]
    );
  }
}

