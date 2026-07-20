#region Purpose
// Declares authorization requirements for a generated FastEndpoint (policy, schemes, and/or roles).
#endregion

#region Design
// Mirrors ASP.NET's AuthorizeAttribute property surface so contracts stay familiar, but is a
// generator-facing marker — not wired through ASP.NET's authorization filter pipeline.
// Absence means the generator emits AllowAnonymous(); presence without Policy/Roles still requires
// auth (FastEndpoints default). Policy maps to FE Policies(...); never emit RequireAuthorization()
// (that API does not exist on EndpointDefinition).
// Lives in TimeWarp.Architecture.Attributes so contract assemblies can annotate without a Roslyn dep.
#endregion

namespace TimeWarp.Architecture.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class EndpointAuthorizeAttribute : Attribute
{
  public string? Policy { get; set; }
  public string? AuthenticationSchemes { get; set; }
  public string? Roles { get; set; }
}
