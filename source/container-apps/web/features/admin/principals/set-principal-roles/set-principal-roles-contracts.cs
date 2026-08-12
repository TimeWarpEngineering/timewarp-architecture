#region Purpose
// Endpoint-centric contract for replacing a principal's stored product roles.
#endregion

#region Design
// Task 147-004: PUT api/admin/principals/{PrincipalId}/roles — full replace (D10 empty allowed →
// next effective = Member). Validator requires each RoleId ∈ RoleIds.All. Handler returns 404
// when the principal is missing. [EndpointAuthorize] (task 182-002): admin.principals.manage
// (PermissionIds); ListPrincipals uses admin.principals.read.
// PrincipalId comes from the route segment (source-generated on partial Command).
// AuthenticationSchemes (task 158): identity-session + mock-identity-session — see
// AuthenticationSchemeNames' Design region.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

/// <summary>Replace stored roles for a principal.</summary>
[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.AdminPrincipalsManage,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class SetPrincipalRoles
{
  [ApiRoute("api/admin/principals/{PrincipalId:guid}/roles", HttpVerb.Put)]
  public sealed partial class Command : IAuthApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }
    public List<Guid> RoleIds { get; set; } = [];
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x).SetValidator(new AuthApiRequestValidator());
      RuleFor(x => x.PrincipalId).NotEmpty();
      RuleFor(x => x.RoleIds).NotNull();
      // Fully qualify product RoleIds — Command.RoleIds property would otherwise shadow the type.
      RuleForEach(x => x.RoleIds)
        .Must(static id => global::TimeWarp.Architecture.Features.RoleIds.All.Contains(id))
        .WithMessage("Each RoleId must be a product role (RoleIds.All).");
    }
  }

  public sealed class Response
  {
    public List<Guid> RoleIds { get; }

    public Response(List<Guid> roleIds)
    {
      RoleIds = roleIds ?? [];
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response([RoleIds.Member, RoleIds.Administrator]);
  }
}
