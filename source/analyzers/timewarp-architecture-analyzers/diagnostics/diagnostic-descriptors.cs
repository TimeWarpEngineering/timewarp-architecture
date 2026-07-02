#region Purpose
// Authoritative registry of the TWE diagnostic ID range for the [ApiEndpoint] generation contract.
#endregion

#region Design
// Centralized so IDs stay unique and stable across the generator and its validators; an ID may
// exist here without a referencing check — reserve the ID first, wire the enforcement separately.
// All are Errors: a violated generation contract yields a missing or broken endpoint at runtime,
// so it must fail the build instead.
#endregion

namespace TimeWarp.Architecture.Analyzers;

internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor ApiEndpointMissingPartial = new(
        id: "TWE001",
        title: "ApiEndpoint class must be partial",
        messageFormat: "Class '{0}' marked with [ApiEndpoint] must be static and partial",
        category: "ApiEndpoint",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

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
        messageFormat: "Route '{0}' with HTTP method '{1}' is already used by '{2}'",
        category: "ApiEndpoint",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ApiEndpointInvalidInterface = new(
        id: "TWE004",
        title: "Invalid interface implementation",
        messageFormat: "Query/Command class must implement IRequest<> and {0}",
        category: "ApiEndpoint",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}