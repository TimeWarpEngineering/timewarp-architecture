#region Purpose
// Marks a contract class for FastEndpoint source generation — the generator emits the endpoint class, so none is hand-written.
#endregion

#region Design
// Lives in a small runtime-attributes assembly, separate from the analyzer/generators, so contract
// projects reference plain attributes without taking a Roslyn dependency. Ships as the public
// TimeWarp.Architecture.Attributes NuGet package (task 092) — not a private dep of Generators.
// Generated endpoints always inherit BaseFastEndpoint (task 131-001 F-005: EndpointType override
// removed as a silent no-op / YAGNI — zero consumers, generic base shape unspecified).
#endregion

namespace TimeWarp.Architecture.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ApiEndpointAttribute : Attribute;
