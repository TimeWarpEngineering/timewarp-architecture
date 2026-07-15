#region Purpose
// Marks a contract class for FastEndpoint source generation — the generator emits the endpoint class, so none is hand-written.
#endregion

#region Design
// Lives in a small runtime-attributes assembly, separate from the analyzer/generators, so contract
// projects reference plain attributes without taking a Roslyn dependency. Ships as the public
// TimeWarp.Architecture.Attributes NuGet package (task 092) — not a private dep of Generators.
// EndpointType optionally overrides the generated endpoint's base class (default BaseFastEndpoint).
#endregion

namespace TimeWarp.Architecture.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ApiEndpointAttribute : Attribute
{
  public Type? EndpointType { get; set; }
}
