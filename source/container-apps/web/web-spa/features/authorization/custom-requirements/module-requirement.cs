namespace TimeWarp.Architecture.CustomRequirements;


public sealed class ModuleRequirement
(
  Guid requiredModule
) : IAuthorizationRequirement
{
  public Guid RequiredModule { get; } = requiredModule;
}
