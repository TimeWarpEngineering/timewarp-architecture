#region Purpose
// Detects duplicate route+verb registrations across generated endpoints (TWE003).
#endregion

#region Design
// Static because incremental-generator source outputs run per symbol with no shared pipeline
// state; the generator resets it at Initialize so entries from a prior compilation cannot
// produce false conflicts. ConcurrentDictionary guards parallel source-output invocations.
// First registration wins — the conflicting endpoint is reported and simply not generated,
// keeping the rest of the compilation usable.
#endregion

namespace TimeWarp.Architecture.Analyzers;

internal sealed class RouteRegistry
{
  private static readonly ConcurrentDictionary<string, (string Route, string HttpVerb, string Endpoint)> RegisteredRoutes = new();

  public static bool TryRegisterRoute(string route, string httpVerb, string endpointName, SourceProductionContext context)
  {
    string key = $"{route}:{httpVerb}";
    
    if (RegisteredRoutes.TryGetValue(key, out (string Route, string HttpVerb, string Endpoint) existing))
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
