#region Purpose
// Endpoint-centric contract for creating a role.
#endregion

#region Design
// [ApiRoute] drives source generation of the FastEndpoint and the Command's route members
// (hence partial). Implementing IRoleDetails lets the Validator compose the shared
// RoleDetailsValidator, so create and update forms enforce identical rules.
// GetMockResponseFactory lets the SPA's MockWebApiService serve this endpoint with no
// backend running; the response echoes a well-known RoleIds constant for determinism.
// [EndpointAuthorize] (task 147-004): Administrator capability via AuthorizationPolicyNames
// (Features substrate — contracts can reference without web-server). [AuthApiRequest] on Command
// remains a client-facing/mock-mode identity signal only; this attribute gates the server.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

[ApiEndpoint]
[EndpointAuthorize(Policy = AuthorizationPolicyNames.CanViewRolesPage)]
public static partial class CreateRole
{
  [ApiRoute("api/Roles", HttpVerb.Post)]
  public sealed partial class Command : IAuthApiRequest, IRoleDetails, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x).SetValidator(new RoleDetailsValidator());
      RuleFor(x => x).SetValidator(new AuthApiRequestValidator());
    }
  }

  public sealed class Response
  {
    public Guid RoleId { get; }

    public Response(Guid roleId)
    {
      RoleId = Guard.Against.NullOrEmpty(roleId);
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response(RoleIds.Administrator);
  }
}
