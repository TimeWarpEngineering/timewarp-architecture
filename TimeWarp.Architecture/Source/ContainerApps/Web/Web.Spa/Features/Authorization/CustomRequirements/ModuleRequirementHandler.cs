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
