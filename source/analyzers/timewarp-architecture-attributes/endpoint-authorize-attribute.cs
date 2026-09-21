#region Purpose
// Declares authorization requirements for a generated FastEndpoint (policy, schemes, and/or roles).
#endregion

#region Design
// Mirrors ASP.NET's AuthorizeAttribute property surface so contracts stay familiar, but is a
// generator-facing marker — not wired through ASP.NET's authorization filter pipeline.
// Absence rule CHANGED in task 110 (fail-closed default): absence of THIS attribute no longer means
// AllowAnonymous() — it means the generator emits nothing, and FastEndpoints requires
// authentication by default. AllowAnonymous() is now emitted ONLY when [EndpointAllowAnonymous] is
// present (and this attribute is not) — see that attribute's Design region. Presence of this
// attribute without Policy/Roles still requires auth with no further restriction (FastEndpoints
// default) — unchanged. Policy maps to FE Policies(...); never emit RequireAuthorization() (that
// API does not exist on EndpointDefinition). TWA0013 flags an [ApiEndpoint] contract carrying
// NEITHER this attribute nor [EndpointAllowAnonymous]; TWA0014 flags both present together, or this
// attribute's absence while [EndpointAllowAnonymous] contradicts an IAuthApiRequest-declaring
// nested Query/Command. TWA0024 (server compilation) flags a named Policy that this host does not
// register via AddPolicy / AddPermissionPolicies.
// AuthenticationSchemes (task 161): emit FE AuthSchemes(...). Required on hosted contracts whose
// named policy has no AddAuthenticationSchemes (PermissionIds via AddPermissionPolicies) — otherwise
// PolicyEvaluator authenticates only the host default scheme and non-default handlers never run.
// Lives in TimeWarp.Architecture.Attributes so contract assemblies can annotate without a Roslyn dep.
#endregion

namespace TimeWarp.Architecture.Attributes;

/// <summary>
/// Declares authorization for a generated FastEndpoint (policy, schemes, and/or roles). This is
/// the sole server-auth marker — <c>IAuthApiRequest</c> is a client/mock signal and does not
/// secure the HTTP shim. Mutually exclusive with <see cref="EndpointAllowAnonymousAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class EndpointAuthorizeAttribute : Attribute
{
  /// <summary>
  /// Named policy passed to FastEndpoints <c>Policies(...)</c>. Must match a policy the hosting
  /// server registers (TWA0024). Leave null to require authentication with no further restriction.
  /// </summary>
  public string? Policy { get; set; }

  /// <summary>
  /// Comma-separated authentication schemes passed to FastEndpoints <c>AuthSchemes(...)</c>.
  /// Required when the named policy has no <c>AddAuthenticationSchemes</c> (for example
  /// permission policies) so non-default handlers still run.
  /// </summary>
  public string? AuthenticationSchemes { get; set; }

  /// <summary>Comma-separated role names the principal must have.</summary>
  public string? Roles { get; set; }
}
