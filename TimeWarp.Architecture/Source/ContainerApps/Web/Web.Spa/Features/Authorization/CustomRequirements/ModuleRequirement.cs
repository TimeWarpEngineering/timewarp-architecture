namespace TimeWarp.Architecture.CustomRequirements;

[UsedImplicitly]
public sealed class ModuleRequirement
(
  Guid requiredModule
) : IAuthorizationRequirement
{
  public Guid RequiredModule { get; } = requiredModule;
}
