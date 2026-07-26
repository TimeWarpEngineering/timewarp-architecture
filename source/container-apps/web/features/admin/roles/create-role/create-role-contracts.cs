#region Purpose
// Endpoint-centric contract for creating a role.
#endregion

#region Design
// [ApiRoute] drives source generation of the FastEndpoint and the Command's route members
// (hence partial). Implementing IRoleDetails lets the Validator compose the shared
// RoleDetailsValidator, so create and update forms enforce identical rules.
// GetMockResponseFactory lets the SPA's MockWebApiService serve this endpoint with no
// backend running; the response echoes a well-known RoleIds constant for determinism.
// [EndpointAuthorize] (task 110): the Command already declared IAuthApiRequest — client-facing
// identity signal only, does NOT secure the server (see the web-api-contracts skill's three-state
// truth table) — this attribute is what actually gates the generated endpoint. Policy is a raw
// string literal because web-contracts cannot reference web-server's IdentitySessionDefaults
// constant (the dependency runs the other way); the comment names the real constant as the source
// of truth. Any authenticated identity-session principal may create demo roles — no admin/role
// policy exists yet (recorded future work, task 110 scope boundary).
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

[ApiEndpoint]
[EndpointAuthorize(Policy = "identity-session-authenticated")] // matches IdentitySessionDefaults.AuthenticatedPolicy
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
