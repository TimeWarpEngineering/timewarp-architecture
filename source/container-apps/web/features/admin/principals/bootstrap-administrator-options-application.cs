#region Purpose
// Break-glass config: force Administrator effective roles for listed PrincipalIds.
#endregion

#region Design
// Primary first-run path is no longer this options bag: CompletePasskeyRegistration claims
// Administrator via IPrincipalRoleStore.TryClaimFirstAdministratorAsync when no admin exists.
// Authentication:BootstrapAdministratorPrincipalIds remains optional break-glass (paste known
// principal ids; union Administrator+Member on resolve without a store write). Empty by default.
// Bound from Authentication section. Features substrate — Identity + Admin without TWA0009.
#endregion

namespace TimeWarp.Architecture.Features;

/// <summary>Optional break-glass Administrator principal ids (effective-role union).</summary>
public sealed class BootstrapAdministratorOptions
{
  /// <summary>PrincipalId Guid strings that always receive Administrator + Member effectively.</summary>
  public string[] BootstrapAdministratorPrincipalIds { get; set; } = [];
}
