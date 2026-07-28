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
// TWA* convention IDs live in the convention-analyzers package, not here.
// Severity: generation-contract violations (TWE002/003/007, TWE005/006) are Errors so a broken
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

  public static readonly DiagnosticDescriptor TypedIdInvalidShape = new(
    id: "TWE006",
    title: "Invalid [TypedId] target",
    messageFormat: "'{0}' is marked [TypedId] but is not a readonly partial record struct; no id surface (New/From/JsonConverter) is generated and JSON serialization would fail open",
    category: "TypedId",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  public static readonly DiagnosticDescriptor ApiEndpointUnknownHttpVerb = new(
    id: "TWE007",
    title: "Unknown or unresolvable HttpVerb",
    messageFormat: "Contract '{0}' declares an unresolvable or unsupported HttpVerb ('{1}'); allowed: Get, Post, Put, Delete, Patch, Head, Options — no endpoint is generated",
    category: "ApiEndpoint",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Fail-closed verb resolution: the generator never defaults an unknown verb to Get.");

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
