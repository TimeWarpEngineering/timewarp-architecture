#region Purpose
// Endpoint-centric contract for fetching a single role to populate an edit form.
#endregion

#region Design
// RoleId is not declared on the Query: the {RoleId:guid} segment in [ApiRoute] makes the
// source generator emit it on the partial, and the min(1) route constraint replaces a
// FluentValidation rule for it. Response implements IRoleDetails so the edit form binds the
// same shape it submits via UpdateRole. PermissionIds (task 182-004) is the role's stored
// permission membership from IRolePermissionStore — display/edit of the bundle, not IRoleDetails.
// GetMockResponseFactory lets the SPA's MockWebApiService serve this endpoint offline with
// deterministic RoleIds data.
// [EndpointAuthorize] (task 182-002): admin.roles.read (PermissionIds); manage is separate.
// AuthenticationSchemes (task 158): identity-session + mock-identity-session — see
// AuthenticationSchemeNames' Design region.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

/// <summary>
/// Get a role by its unique identifier for possible editing.
/// </summary>
[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.AdminRolesRead,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class GetRole
{
  [ApiRoute("api/Roles/{RoleId:guid}", HttpVerb.Get)]
  public sealed partial class Query : IAuthApiRequest, IQueryStringRouteProvider, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }

    // GET carries no body, so UserId travels in the query string (manual IAuthApiRequest form —
    // the [AuthApiRequest] attribute's generated helper is not available here).
    public string GetRouteWithQueryString() =>
      $"{GetRoute()}?{this.GetQueryString(new NameValueCollection { { nameof(UserId), UserId.ToString() } })}";
  }

  public sealed class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
      RuleFor(x => x).SetValidator(new AuthApiRequestValidator());
    }
  }

  public sealed class Response : IRoleDetails
  {
    public Guid RoleId { get; }
    public string Name { get; set; }
    public string Description { get; set; }

    /// <summary>Stored permission ids for this role (empty when none granted).</summary>
    public List<string> PermissionIds { get; }

    public Response
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
    return _ =>
      new Response
      (
        roleId: RoleIds.Administrator,
        name: nameof(RoleIds.Administrator),
        description: "The Administrator role is for administrators. And has access to all modules.",
        permissionIds:
        [
          PermissionIds.AdminAccess,
          PermissionIds.AdminRolesRead,
          PermissionIds.AdminRolesManage,
          PermissionIds.AdminPrincipalsRead,
          PermissionIds.AdminPrincipalsManage,
          PermissionIds.ProfileRead,
          PermissionIds.SettingsRead,
        ]
      );
  }
}
