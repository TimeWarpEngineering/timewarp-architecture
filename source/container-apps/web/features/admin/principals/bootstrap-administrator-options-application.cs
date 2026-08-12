#region Purpose
// Configuration for first-run Administrator bootstrap without auto-promoting registrants.
#endregion

#region Design
// Task 147-004 D3: Authentication:BootstrapAdministratorPrincipalIds string[] of PrincipalId
// Guid values. When a principal's id is listed, IEffectiveRolesResolver unions Administrator +
// Member onto effective roles (in addition to any stored assignment). Empty by default —
// operators paste known principal ids into Development config after first registration.
// Bound from Authentication section (Authentication:BootstrapAdministratorPrincipalIds).
// Features substrate namespace — shared by Identity session + Admin without TWA0009.
#endregion

namespace TimeWarp.Architecture.Features;

/// <summary>Development bootstrap of Administrator without first-registrant auto-promote.</summary>
public sealed class BootstrapAdministratorOptions
{
  /// <summary>PrincipalId Guid strings that always receive Administrator + Member effectively.</summary>
  public string[] BootstrapAdministratorPrincipalIds { get; set; } = [];
}
