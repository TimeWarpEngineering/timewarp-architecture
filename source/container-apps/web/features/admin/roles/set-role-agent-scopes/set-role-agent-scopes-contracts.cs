#region Purpose
// Endpoint-centric contract for replacing a role's agent-scope list.
#endregion

#region Design
// Task 466: PUT api/Roles/{RoleId}/agent-scopes — full replace, beside SetRolePermissions.
// The list is agent scopes (AgentScopes.All), not human permission ids. Empty clears the role's
// contribution. identity:read stays approved at issuance even when it is absent here.
// [EndpointAuthorize] uses admin.roles.manage, same as SetRolePermissions.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

using TimeWarp.Identity;

/// <summary>Replace stored agent scopes for a role.</summary>
[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.AdminRolesManage,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class SetRoleAgentScopes
{
  [ApiRoute("api/Roles/{RoleId:guid}/agent-scopes", HttpVerb.Put)]
  [Operation("roles.set-agent-scopes", Description = "Replace a role's agent scopes.", Group = "admin.roles")]
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
      RuleFor(command => command.RoleId).NotEmpty();
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
    return _ => new Response([AgentScopes.IdentityRead, AgentScopes.FeedbackRead]);
  }
}
