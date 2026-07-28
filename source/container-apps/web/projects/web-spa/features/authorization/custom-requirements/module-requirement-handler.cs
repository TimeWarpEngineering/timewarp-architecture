#region Purpose
// Satisfies ModuleRequirement policies from the user's fetched module grants.
#endregion

#region Design
// Checks AuthorizationState instead of claims: module grants come from the app's API and
// would otherwise have to be stuffed into the ClaimsPrincipal.
// Never calls context.Fail — an unmet requirement stays unresolved so another handler for
// the same requirement can still succeed.
// State is read per evaluation, so decisions reflect grants fetched or cleared at any time.
#endregion

namespace TimeWarp.Architecture.CustomRequirements;

public class ModuleRequirementHandler
(
  IStore Store
) : AuthorizationHandler<ModuleRequirement>
{
  private AuthorizationState AuthorizationState => Store.GetState<AuthorizationState>();

  protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ModuleRequirement requirement)
  {
    bool hasModule = AuthorizationState.Modules != null && AuthorizationState.Modules.Contains(requirement.RequiredModule);

    if (hasModule) context.Succeed(requirement);

    return Task.CompletedTask;
  }
}
