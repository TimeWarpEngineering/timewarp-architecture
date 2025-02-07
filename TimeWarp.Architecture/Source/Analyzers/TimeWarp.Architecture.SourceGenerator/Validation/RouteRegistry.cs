using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;

namespace TimeWarp.Architecture.SourceGenerator;

internal class RouteRegistry
{
    private static readonly ConcurrentDictionary<string, (string Route, string HttpVerb, string Endpoint)> RegisteredRoutes = new();

    public static bool TryRegisterRoute(string route, string httpVerb, string endpointName, SourceProductionContext context)
    {
        var key = $"{route}:{httpVerb}";
        
        if (RegisteredRoutes.TryGetValue(key, out var existing))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.ApiEndpointRouteConflict,
                    Location.None,
                    route,
                    httpVerb,
                    existing.Endpoint));
            return false;
        }

        RegisteredRoutes.TryAdd(key, (route, httpVerb, endpointName));
        return true;
    }

    public static void Reset()
    {
        RegisteredRoutes.Clear();
    }
}