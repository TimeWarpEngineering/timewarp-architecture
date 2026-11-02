#region Purpose
// Endpoint-centric contract for replacing a principal's own agent-scope grants.
#endregion

#region Design
// Task 466: PUT api/admin/principals/{PrincipalId}/agent-scopes — full replace, beside
// SetPrincipalRoles. These grants sit on top of role agent scopes. Empty clears only the
// principal's own grants. identity:read stays approved either way. The catalog is
// AgentScopes.All. [EndpointAuthorize] uses admin.principals.manage.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

using TimeWarp.Identity;

/// <summary>Replace agent scopes granted directly on a principal.</summary>
[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.AdminPrincipalsManage,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class SetPrincipalAgentScopes
{
  [ApiRoute("api/admin/principals/{PrincipalId:guid}/agent-scopes", HttpVerb.Put)]
  [Operation(
    "principals.set-agent-scopes",
    Description = "Replace agent scopes granted on a principal.",
    Group = "admin.principals")]
  public sealed partial class Command : IAuthApiRequest, IIdempotentCommand<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }
    public List<string> Scopes { get; set; } = [];
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(command => command).SetValidator(new AuthApiRequestValidator());
      RuleFor(command => command.PrincipalId).NotEmpty();
      RuleFor(command => command.Scopes).NotNull();
      RuleForEach(command => command.Scopes)
        .Must(static scope => AgentScopes.All.Contains(scope, StringComparer.Ordinal))
        .WithMessage("Each scope must be a known agent scope (AgentScopes.All).");
    }
  }

  public sealed class Response
  {
    public List<string> Scopes { get; }

    public Response(List<string> scopes)
    {
      Scopes = scopes ?? [];
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response([AgentScopes.FeedbackRead]);
  }
}
