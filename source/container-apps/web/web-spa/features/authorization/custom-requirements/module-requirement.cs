#region Purpose
// Carries the module Guid an authorization policy demands; ModuleRequirementHandler satisfies it from fetched module grants.
#endregion

namespace TimeWarp.Architecture.CustomRequirements;

public sealed class ModuleRequirement
(
  Guid requiredModule
) : IAuthorizationRequirement
{
  public Guid RequiredModule { get; } = requiredModule;
}
