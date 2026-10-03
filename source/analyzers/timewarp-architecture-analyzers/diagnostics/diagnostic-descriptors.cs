#region Purpose
// Authoritative registry of TWE / SG diagnostic IDs for TimeWarp Architecture generators.
#endregion

#region Design
// Centralized so IDs stay unique and stable across generators. F-014 (task 131-001): this file is
// the SSOT for TWE/SG — page/typed-id/FastEndpoint/ingress generators reference these descriptors
// rather than declaring private copies (SG001 was previously dual-declared).
// TWE001 / TWE004 were never reported and are deleted (IDs reserved — do not reuse without a
// deliberate new meaning). TWE002 (missing Query/Command) and TWE007 (unknown verb) are wired in
// FastEndpointSourceGenerator; TWE003 is per-compilation route conflict (all parties, no emit).
// TWE008 is the FastEndpoint fail-closed allow-list (TWA0019-style): EnableApiEndpointGeneration
// requires ApiEndpointContractAssemblies AssemblyName values that match marked referenced
// assemblies — empty/typo/unmarked must not silently emit nothing.
// TWA* convention IDs live in the convention-analyzers package, not here (ingress TWA0017–0019
// are the historical exception, declared on IngressRoutePrefixGenerator).
// TWE009 is the [Page] Navigable opt-in contract: a registry destination needs a static route and
// a literal bool (syntax-keyed generator), otherwise the page would silently miss the registry.
// TWE010/TWE011 are the [Page] multi-route contract (task 096): two route templates on one page
// that Blazor would treat as the same route (TWE010), and route declarations the generator cannot
// emit faithfully — stacked [Page], a non-literal additional route, or a token whose type disagrees
// with an earlier route of the page (TWE011). Both are fail-closed: the page surface is not generated.
// Severity: generation-contract violations (TWE002/003/007/008/009/010/011, TWE005/006) are Errors so a broken
// endpoint/page/id fails the build; SG* are Warnings (resilience / missing deps / log).
#endregion

namespace TimeWarp.Architecture.Analyzers;

internal static class DiagnosticDescriptors
{
  // ── TWE: generation contracts ────────────────────────────────────────────

  public static readonly DiagnosticDescriptor ApiEndpointMissingQuery = new(
    id: "TWE002",
    title: "Missing Query/Command class",
    messageFormat: "No Query or Command class found in {0}",
    category: "ApiEndpoint",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  public static readonly DiagnosticDescriptor ApiEndpointRouteConflict = new(
    id: "TWE003",
    title: "Route conflict detected",
    messageFormat: "Route '{0}' with HTTP method '{1}' is claimed by multiple [ApiEndpoint] contracts: {2}",
    category: "ApiEndpoint",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Each route+verb pair may be hosted by at most one contract. All parties in a conflict group are reported and none of them are generated.");

  public static readonly DiagnosticDescriptor PageInvalidPolicy = new(
    id: "TWE005",
    title: "Invalid [Page] Policy argument",
    messageFormat: "[Page] Policy must be a const field reference (e.g. Policies.SettingsEdit), not a string literal, nameof(...), or other expression. Omit Policy for Policies.Anonymous.",
    category: "Page",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Pit of success: product policy constants are the single source of truth for registered policy names. Identifier glue and string literals silently mis-authorize.");

  public static readonly DiagnosticDescriptor PageInvalidNavigable = new(
    id: "TWE009",
    title: "Invalid [Page] Navigable opt-in",
    messageFormat: "[Page] Navigable on '{0}' {1}; the page is not added to PageRegistry",
    category: "Page",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Fail-closed: a navigation destination must be reachable without arguments (static route, IStaticRoute) and the opt-in must be the literal true or false.");

  public static readonly DiagnosticDescriptor PageDuplicateRoute = new(
    id: "TWE010",
    title: "Duplicate [Page] route",
    messageFormat: "Route '{1}' on '{0}' duplicates another route of the same page; no page surface is generated",
    category: "Page",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Fail-closed: two routes of one page that differ only by case or token names are the same Blazor route (ambiguous match at runtime). Applies to the primary route, [Page] additional routes, and hand-written [Route] attributes on the same declaration.");

  public static readonly DiagnosticDescriptor PageConflictingRouteDeclaration = new(
    id: "TWE011",
    title: "Conflicting [Page] route declaration",
    messageFormat: "[Page] on '{0}' {1}; no page surface is generated",
    category: "Page",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Fail-closed: one [Page] per page, with the primary route first and additional routes as further string-literal arguments; a route token shared with an earlier route keeps that route's type.");

  public static readonly DiagnosticDescriptor TypedIdInvalidShape = new(
    id: "TWE006",
    title: "Invalid [TypedId] target",
    messageFormat: "'{0}' is marked [TypedId] but is not a readonly partial record struct; no id surface (New/From/JsonConverter) is generated and JSON serialization would fail open",
    category: "TypedId",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  public static readonly DiagnosticDescriptor ApiEndpointUnknownHttpVerb = new(
    id: "TWE007",
    title: "Unresolvable route or HttpVerb",
    messageFormat: "Contract '{0}' has an unresolvable route or HttpVerb ('{1}'); require [ApiRoute] with a non-empty template and a verb in: Get, Post, Put, Delete, Patch, Head, Options — no endpoint is generated",
    category: "ApiEndpoint",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Fail-closed: missing/empty ApiRoute or unknown verb never defaults to Get or silent skip.");

  public static readonly DiagnosticDescriptor ApiEndpointContractAssembliesInvalid = new(
    id: "TWE008",
    title: "ApiEndpointContractAssemblies is missing or does not match a marked contracts assembly",
    messageFormat: "EnableApiEndpointGeneration requires ApiEndpointContractAssemblies as an allow-list of AssemblyName values: {0}",
    category: "ApiEndpoint",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Fail-closed: an empty, mistyped, or unmarked allow-list must not silently emit no endpoints. Use the AssemblyName (web-contracts, api-contracts), not the namespace.");

  // ── SG: generator logs / resilience ──────────────────────────────────────

  public static readonly DiagnosticDescriptor SourceGeneratorLog = new(
    id: "SG001",
    title: "Source Generator Log",
    messageFormat: "{0}",
    category: "SourceGenerator",
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true);

  public static readonly DiagnosticDescriptor MissingFastEndpoints = new(
    id: "SG002",
    title: "Missing FastEndpoints dependencies",
    messageFormat: "EnableApiEndpointGeneration is set to true, but FastEndpoints or BaseFastEndpoint could not be found in the compilation. Ensure the api feature and required packages are referenced.",
    category: "SourceGenerator",
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true);

  public static readonly DiagnosticDescriptor TypedIdBclGeneratorError = new(
    id: "SG010",
    title: "TypedId generator error",
    messageFormat: "Error generating TypedId BCL surface for {0}: {1}",
    category: "SourceGenerator",
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true);

  public static readonly DiagnosticDescriptor TypedIdEfGeneratorError = new(
    id: "SG011",
    title: "TypedId EF generator error",
    messageFormat: "Error generating TypedId EF converters: {0}",
    category: "SourceGenerator",
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true);
}
