#region Purpose
// ASP.NET AuthorizationHandler for PermissionRequirement via IPermissionEvaluator.
#endregion

#region Design
// Task 182-002: always routes through IPermissionEvaluator — never inspects role claims or
// permission claim bags. PrincipalId comes from IdentitySessionDefaults.PrincipalIdClaimType
// (timewarp:principal_id); AuthenticationType is the scheme passed to the evaluator so
// agent-token expands scopes and human schemes expand roles (182-006). Never calls context.Fail —
// unmet requirement stays
// unresolved so another handler could still succeed (ASP.NET multi-handler composition).
// Filename avoids registered function segment "-handler-" (TWA0015 pairs it with -application);
// type remains PermissionRequirementHandler. Scoped DI: depends on scoped IPermissionEvaluator.
// Note (182-003): Web.Spa.Program.ConfigureServices is composed into web-server for prerender —
// SPA PolicyRegistration must not overwrite these PermissionRequirement policies with
// RequireClaim (see SPA policy-registration.cs Design).
#endregion

namespace TimeWarp.Architecture.Features;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Identity;

/// <summary>Authorization handler for <see cref="PermissionRequirement"/>.</summary>
public sealed class PermissionRequirementHandler : AuthorizationHandler<PermissionRequirement>
{
  private readonly IPermissionEvaluator PermissionEvaluator;

  public PermissionRequirementHandler(IPermissionEvaluator permissionEvaluator)
  {
    PermissionEvaluator = permissionEvaluator
      ?? throw new ArgumentNullException(nameof(permissionEvaluator));
  }

  protected override async Task HandleRequirementAsync(
    AuthorizationHandlerContext context,
    PermissionRequirement requirement)
  {
    ArgumentNullException.ThrowIfNull(context);
    ArgumentNullException.ThrowIfNull(requirement);

    string? claimValue = context.User.FindFirstValue(IdentitySessionDefaults.PrincipalIdClaimType);
    if (!Guid.TryParse(claimValue, out Guid guid) || guid == Guid.Empty)
    {
      return;
    }

    string? authenticationScheme = context.User.Identity?.AuthenticationType;
    var principalId = PrincipalId.From(guid);

    bool hasPermission = await PermissionEvaluator
      .HasPermissionAsync(principalId, authenticationScheme, requirement.PermissionId)
      .ConfigureAwait(false);

    if (hasPermission)
    {
      context.Succeed(requirement);
    }
  }
}
